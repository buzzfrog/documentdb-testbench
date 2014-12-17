documentdb-testbench
====================

A test bench for DocumentDB. Includes possibility to generate large collections of documents.

Features:
* Create Databases, Collections
* Fill a Collection with Documents
* Test Update and Delete
* Test Queries

Generated Documents:
    {
        "id": "84e0d2ad-f41e-47a3-94d0-6c3534a3ca65",
        "FirstName": "Mathilda",
        "LastName": "Sundqvist",
        "Age": 82,
        "FavoriteColor": "Green",
        "Gender": "F"
    }

id - Could be an integer or an Guid. (Easier to use Guid when creating a large collection of documents)
FirstName and LastName - All names is imported from a Swedish name database, so it will be (maybe) strange names and strange characters.




