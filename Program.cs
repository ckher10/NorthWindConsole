using NLog;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using NorthWindConsole.Model;
using System.ComponentModel.DataAnnotations;
using Microsoft.IdentityModel.Tokens;
using System.Text.RegularExpressions;
string path = Directory.GetCurrentDirectory() + "//nlog.config";

// create instance of Logger
var logger = LogManager.Setup().LoadConfigurationFromFile(path).GetCurrentClassLogger();

logger.Info("Program started");

var configuration = new ConfigurationBuilder()
        .AddJsonFile($"appsettings.json");

var config = configuration.Build();

var db = new DataContext();

do
{
  Console.WriteLine("1) Display categories");
  Console.WriteLine("2) Add category");
  Console.WriteLine("3) Display Category and related products");
  Console.WriteLine("4) Display all Categories and their related products");
  Console.WriteLine("5) Display products");
  Console.WriteLine("6) Find Product");
  Console.WriteLine("7) Add Product");
  Console.WriteLine("Enter to quit");
  string? choice = Console.ReadLine();
  Console.Clear();
  logger.Info("Option {choice} selected", choice);

  if (choice == "1")
  {
    // display categories

    var query = db.Categories.OrderBy(p => p.CategoryName);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"{query.Count()} records returned");
    Console.ForegroundColor = ConsoleColor.Magenta;
    foreach (var item in query)
    {
      Console.WriteLine($"{item.CategoryName} - {item.Description}");
    }
    Console.ForegroundColor = ConsoleColor.White;
  }
  else if (choice == "2")
  {
    // Add category
    Category category = new();
    Console.WriteLine("Enter Category Name:");
    category.CategoryName = Console.ReadLine()!;
    Console.WriteLine("Enter the Category Description:");
    category.Description = Console.ReadLine();
    ValidationContext context = new ValidationContext(category, null, null);
    List<ValidationResult> results = new List<ValidationResult>();

    var isValid = Validator.TryValidateObject(category, context, results, true);
    if (isValid)
    {
      // check for unique name
      if (db.Categories.Any(c => c.CategoryName == category.CategoryName))
      {
        // generate validation error
        isValid = false;
        results.Add(new ValidationResult("Name exists", ["CategoryName"]));
      }
      else
      {
        logger.Info("Validation passed");
        // TODO: save category to db
      }
    }
    if (!isValid)
    {
      foreach (var result in results)
      {
        logger.Error($"{result.MemberNames.First()} : {result.ErrorMessage}");
      }
    }
  }
  else if (choice == "3")
  {
    var query = db.Categories.OrderBy(p => p.CategoryId);

    Console.WriteLine("Select the category whose products you want to display:");
    Console.ForegroundColor = ConsoleColor.DarkRed;
    foreach (var item in query)
    {
      Console.WriteLine($"{item.CategoryId}) {item.CategoryName}");
    }
    Console.ForegroundColor = ConsoleColor.White;
    int id = int.Parse(Console.ReadLine()!);
    Console.Clear();
    logger.Info($"CategoryId {id} selected");
    Category category = db.Categories.Include("Products").FirstOrDefault(c => c.CategoryId == id)!;
    Console.WriteLine($"{category.CategoryName} - {category.Description}");
    foreach (Product p in category.Products)
    {
      Console.WriteLine($"\t{p.ProductName}");
    }
  }
  else if (choice == "4")
  {
    var query = db.Categories.Include("Products").OrderBy(p => p.CategoryId);
    foreach (var item in query)
    {
      Console.WriteLine($"{item.CategoryName}");
      foreach (Product p in item.Products)
      {
        Console.WriteLine($"\t{p.ProductName}");
      }
    }
  }
  else if (choice == "5")
  {
    // display products
    string? productOption;
    do
    {
      Console.WriteLine("1) All Products");
      Console.WriteLine("2) Active Products");
      Console.WriteLine("3) Discontinued Products");
      Console.WriteLine("Enter to go back");
      productOption = Console.ReadLine();
      if (productOption.IsNullOrEmpty()) break;
      if (!Regex.IsMatch(productOption, @"^[1-3]$"))
        Console.WriteLine("\nPlease enter a valid input");
    } while (!Regex.IsMatch(productOption, @"^[1-3]$"));
    if (productOption.IsNullOrEmpty()) continue;

    IOrderedQueryable<Product>? query = db.Products.OrderBy(p => p.ProductName);
    if (productOption == "2") query = (IOrderedQueryable<Product>)db.Products.Where(p => !p.Discontinued);
    if (productOption == "3") query = (IOrderedQueryable<Product>)db.Products.Where(p => p.Discontinued);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"{query.Count()} records returned");
    Console.ForegroundColor = ConsoleColor.Magenta;
    foreach (var item in query)
    {
      if (item.Discontinued)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{item.ProductName}");
        Console.ForegroundColor = ConsoleColor.Magenta;
      }
      else Console.WriteLine($"{item.ProductName}");
    }
    Console.ForegroundColor = ConsoleColor.White;
  }
  else if (choice == "6")
  {
    //Find products
    string? search = "";
    do
    {
      Console.WriteLine("Enter Product ID or press enter to go back");
      Console.Write("Search ID: ");
      search = Console.ReadLine();
      if (search.IsNullOrEmpty()) break;
      if (!db.Products.Any(p => p.ProductId == Convert.ToInt32(search)))
        logger.Info("ID does not exist");
      if (!Regex.IsMatch(search, @"^\d+$"))
        logger.Info("Please input valid ID");
    } while (!Regex.IsMatch(search, @"^\d+$") || !db.Products.Any(p => p.ProductId == Convert.ToInt32(search)));
    if (search.IsNullOrEmpty()) continue;

    var query = db.Products.Where(p => p.ProductId == Convert.ToInt32(search));

    Console.ForegroundColor = ConsoleColor.Magenta;
    if (query.First().Discontinued) Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"ID: {query.First().ProductId}");
    Console.WriteLine($"Name: {query.First().ProductName}");
    Console.WriteLine($"Category ID:{query.First().CategoryId}");
    Console.WriteLine($"Supplier ID:{query.First().SupplierId}");
    Console.WriteLine($"Price: {query.First().UnitPrice}");
    Console.WriteLine($"Quantity Per Unit: {query.First().QuantityPerUnit}");
    Console.WriteLine($"Units in Stock: {query.First().UnitsInStock}");
    Console.WriteLine($"Units On Order: {query.First().UnitsOnOrder}");
    Console.WriteLine($"Reorder Level: {query.First().ReorderLevel}");
    Console.WriteLine($"Discontinued: {query.First().Discontinued}");
    Console.ForegroundColor = ConsoleColor.White;
  }
  else if (choice == "7")
  {
    //Add Product
    Product product = new();
    var suppliers = db.Suppliers;
    var categories = db.Categories;
    Console.WriteLine("Enter Product Name: ");
    product.ProductName = Console.ReadLine()!;

    foreach (var supplier in suppliers) Console.WriteLine($"{supplier.SupplierId} | {supplier.CompanyName}");

    bool supplierIDExists = false;
    string? productInput = "";
    do
    {
      Console.WriteLine("Enter the Supplier ID: ");
      productInput = Console.ReadLine();
      if (!Regex.IsMatch(productInput, @"^\d+$")) {
        logger.Info("Input must be a number");
        continue;
      }
      product.SupplierId = Convert.ToInt32(productInput);
      supplierIDExists = suppliers.Any(p => p.SupplierId == Convert.ToInt32(product.SupplierId));
      if (!supplierIDExists)
      {
        logger.Info("ID does not exist");
      }
    } while (!supplierIDExists);

    ValidationContext context = new ValidationContext(product, null, null);
    List<ValidationResult> results = new List<ValidationResult>();

    var isValid = Validator.TryValidateObject(product, context, results, true);
    // if (isValid)
    // {
    //   // check for unique name
    //   if (db.Categories.Any(c => c.CategoryName == category.CategoryName))
    //   {
    //     // generate validation error
    //     isValid = false;
    //     results.Add(new ValidationResult("Name exists", ["CategoryName"]));
    //   }
    //   else
    //   {
    //     logger.Info("Validation passed");
    //     // TODO: save category to db
    //   }
    // }
    // if (!isValid)
    // {
    //   foreach (var result in results)
    //   {
    //     logger.Error($"{result.MemberNames.First()} : {result.ErrorMessage}");
    //   }
    // }
  }
  else if (String.IsNullOrEmpty(choice))
  {
    break;
  }
  Console.WriteLine();
} while (true);

logger.Info("Program ended");