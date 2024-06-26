// See https://aka.ms/new-console-template for more information


using AutoProperty.Sample;

var book = new Book()
{
    Title = "The Hobbit",
    Author = "J.R.R. Tolkien",
    LastUpdated = DateTimeOffset.Now
};

Console.WriteLine(book);