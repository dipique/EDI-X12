using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace EDI_Enrollment
{
    /// <summary>
    /// From a delimited text file, translates each row to an object of type T
    /// 
    /// TODO: Right now, data annotations are just a form of documentation; however, this import
    ///       file SHOULD look at the data annotations and mark records valid or invalid
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ImportFile<T> where T : class
    {
        private int DataStartRow = 5;
        private int DataStartCol = 2;
        private char delim = '\t';
        const int MAX_RECURSION_DEPTH = 3;

        const string supportedTypes = "string|int32|datetime"; //each has to be coded for. Enums are also allowed.

        public List<T> Import(string filename)
        {
            string[] lines = File.ReadAllLines(filename).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            string[] fieldAddresses = lines[0].Split(delim).Skip(DataStartCol - 1).ToArray();

            //set up all the fields
            List<Field<T>> FieldSet = new List<Field<T>>();
            for (int x = 0; x < fieldAddresses.Count(); x++)
                FieldSet.Add(new Field<T>(fieldAddresses[x], x));

            //local function to assign to current value
            List<T> retVal = new List<T>();
            T currentItem = default(T);
            string[] data = null;
            void SetValues(object objToSet, List<AddressLevel> fullAddress)
            {
                //loop through properties of object recursively
                foreach (var prop in objToSet.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                                                       .Where(p => p.GetSetMethod() != null))
                {
                    //determine whether this is a field to set or one that requires a recursion layer
                    var type = prop.PropertyType;
                    var destField = supportedTypes.Contains(type.Name.ToLower()) || type.IsEnum;

                    /////////
                    //Check for a specific property -- for debugging
                    if (prop.Name == "ElectedUnits")
                    {

                    }
                    /////////

                    //see if we have data to assign
                    var newFullAddress = fullAddress.Count() == 0 ? prop.Name
                                                                  : $"{string.Join(".", fullAddress.Select(al => al.NameWithIndex))}.{prop.Name}";
                    var fieldData = FieldSet.FirstOrDefault(f => f.AddressWithIndexes == newFullAddress);

                    //if it's a destination field and we have data, set it
                    if (destField)
                    {
                        //if we have data, go ahead and set it
                        if (fieldData != null)
                            fieldData.Set(prop, objToSet, data);

                        //then move to the next field
                        continue;
                    }

                    //the property is an object that requires deeper recursion.  Check our level of recursion.
                    int recursionDepth = fullAddress.Count();
                    if (recursionDepth >= MAX_RECURSION_DEPTH) return;

                    //Check if it's a collection, which requires special treatment
                    var isCollection = type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);

                    //if not, we start the next recursion
                    if (!isCollection)
                    {
                        var newItem = Activator.CreateInstance(prop.PropertyType);
                        var newAddress = fullAddress.Select(fa => new AddressLevel(fa.NameWithIndex)).ToList();
                        newAddress.Add(new AddressLevel(prop.Name));
                        SetValues(newItem, newAddress);

                        //only add if the object has data
                        if (!ObjectUnchanged(newItem))
                        {
                            prop.SetValue(objToSet, newItem);
                        }
                    }
                    else
                    {
                        //if it's a collection, we need to find out how many we should create
                        int createCount = 0;
                        while (FieldSet.Where(f => f.FullAddress.Count() > recursionDepth)
                                       .Select(f => f.FullAddress[recursionDepth])
                                       .Any(fl => fl.ObjectName == prop.Name &&
                                                  fl.Index == (createCount + 1)))
                        {
                            createCount++;
                        }

                        //if the answer is 0, move on to the next property
                        if (createCount == 0) continue;

                        //otherwise, create an instance of each item, then start it on its merry recursion
                        var addMethod = prop.PropertyType.GetMethod("Add");
                        var currentIndex = 1;
                        for (int y = 0; y < createCount; y++)
                        {
                            var newAddress = fullAddress.Select(fa => new AddressLevel(fa.NameWithIndex)).ToList();
                            newAddress.Add(new AddressLevel(prop.Name, currentIndex));
                            var newItem = Activator.CreateInstance(prop.PropertyType.GetGenericArguments()[0]);
                            SetValues(newItem, newAddress);

                            //only add if the object has data
                            if (!ObjectUnchanged(newItem))
                            {
                                addMethod.Invoke(prop.GetValue(objToSet), new object[] { newItem });
                                currentIndex++;
                            }                            
                        }
                    }
                }
            }

            //process data and generate the return items
            var dataLines = lines.Skip(DataStartRow - 1);
            foreach (var dataLine in dataLines)
            {
                data = dataLine.Split(delim).Skip(DataStartCol - 1).ToArray();
                currentItem = (T)Activator.CreateInstance(typeof(T));
                SetValues(currentItem, new List<AddressLevel>());
                retVal.Add(currentItem);
            }

            return retVal;
        }

        /// <summary>
        /// note: this doesn't check for inequality on complex objects that are nested within the parent object
        ///       unless they have a suitably overloaded ToString method for equality comparison
        /// </summary>
        /// <param name="objToCheck"></param>
        /// <returns></returns>
        public bool ObjectUnchanged(object objToCheck)
        {
            if (objToCheck == null) return true;
            var unchanged = Activator.CreateInstance(objToCheck.GetType());
            foreach (var prop in objToCheck.GetType().GetProperties(BindingFlags.Instance
                                                                  | BindingFlags.Public 
                                                                  | BindingFlags.FlattenHierarchy)
                                                     .Where(p => p.GetSetMethod() != null))
            {
                //look for a changed property
                if ((prop.GetValue(unchanged)?.ToString() ?? string.Empty) != (prop.GetValue(objToCheck)?.ToString() ?? string.Empty)) return false;
            }

            return true;
        }
    }

    public class Field<T>
    {
        const char delim = '.';

        public AddressLevel[] FullAddress { get; set; }
        public int ColumnIndex { get; set; }

        public Field(string address, int index)
        {
            ColumnIndex = index;
            FullAddress = address.Split(delim).Select(lvl => new AddressLevel(lvl)).ToArray();
        }

        public void Set(PropertyInfo prop, object dest, string[] data)
        {
            var value = data.Count() > ColumnIndex ? data[ColumnIndex] : string.Empty;
            if (string.IsNullOrEmpty(value)) return;

            object setValue = value;

            //get the set value out of it's string format
            if (prop.PropertyType.IsEnum) {
                setValue = Enum.Parse(prop.PropertyType, value);
            }
            else if (prop.PropertyType == typeof(int)) {
                setValue = int.Parse(value);
            }
            else if (prop.PropertyType == typeof(DateTime)) {
                setValue = DateTime.Parse(value);
            }

            prop.SetValue(dest, setValue);
        }

        public string Address => string.Join(".", FullAddress.Select(fa => fa.ObjectName));
        public string AddressWithIndexes => string.Join(".", FullAddress.Select(fa => fa.NameWithIndex));
    }

    public class AddressLevel
    {
        private char collection_ind = ']';
        private char col_start_ind = '[';

        public string NameWithIndex { get; set; }
        public string ObjectName { get; set; }
        public int Index { get; set; } = -1;

        public AddressLevel(string levelText)
        {
            //if it's blank, you just have an empty address level
            if (string.IsNullOrWhiteSpace(levelText)) return;

            NameWithIndex = levelText;
            ObjectName = levelText;
            if (levelText[levelText.Length - 1] == collection_ind)
            {
                var colStartIndex = levelText.IndexOf(col_start_ind);
                Index = int.Parse(levelText.Substring(colStartIndex + 1, levelText.Length - colStartIndex - 2));
                ObjectName = levelText.Substring(0, colStartIndex);
            }
        }

        public AddressLevel(string levelName, int index)
        {
            NameWithIndex = $"{levelName}{col_start_ind}{index}{collection_ind}";
            Index = index;
            ObjectName = levelName;
        }

        public bool IsCollection => Index > -1;
    }
}
