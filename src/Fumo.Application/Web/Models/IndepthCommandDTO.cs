﻿namespace Fumo.Application.Web.Models;

public class IndepthCommandDTO
{
    public string Regex { get; set; }

    public List<string> Permission { get; set; }

    public string Description { get; set; }
}