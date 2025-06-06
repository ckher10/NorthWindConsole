﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NorthWindConsole.Model;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;
    [Required(ErrorMessage = "YO - Enter the name!")]

    public string? Description { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
