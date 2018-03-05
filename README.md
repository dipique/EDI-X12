# EDI-X12
Rough process for generating EDI X12 834 and 837 files from a CSV file. It is not a flexible implementation (i.e. it doesn't provide particularly easy paths to READING EDI files, and the actual generation is a little clunky relative to, for example, using an XML template); however, it IS very flexible and allows for very easy changes to specific values based on arbitrary code needed for your business case.

For testing (or hell, for production, who am I to say) it has the ability to translate CSV data into intermediate CLR objects, then from those objects to X12 files, and those steps are totally separate so you can easily use the code to, for example, pull from a database instead of text files.

The CSV translation uses Reflection and the CSV headers to populate values in any CLR object. It supports multi-level objects, arrays, etc. The line of code used to read from the CLR file is:

List<Claim> claims = new ImportFile<Claim>().Import(IMPORT_FILENAME);

However, the code is structured in a way that allows easy transition to other data sources.

Please credit me if you use any of this code.
