// See https://aka.ms/new-console-template for more information


using AutoProperty.Sample;

var book = new Book()
{
    Title = "The Hobbit",
    Author = "J.R.R. Tolkien",
    LastUpdated = DateTimeOffset.Now
};

var author = new Author()
{
    Name = "J.R.R. Tolkien",
};

Console.WriteLine(book);
Console.WriteLine(author);