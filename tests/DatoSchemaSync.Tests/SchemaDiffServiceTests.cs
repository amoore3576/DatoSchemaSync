using DatoSchemaSync.Models;
using DatoSchemaSync.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using FluentAssertions;

namespace DatoSchemaSync.Tests;

public class SchemaDiffServiceTests
{
    private readonly SchemaDiffService _sut = new(NullLogger<SchemaDiffService>.Instance);

    [Fact]
    public void ComputeDiff_EmptySchemas_ReturnsNoChanges()
    {
        var diff = _sut.ComputeDiff(new DatoSchema(), new DatoSchema());
        diff.HasChanges.Should().BeFalse();
    }

    [Fact]
    public void ComputeDiff_NewLocale_DetectsAddition()
    {
        var previous = new DatoSchema();
        var current = new DatoSchema
        {
            Locales = new List<DatoLocale>
            {
                new() { Id = "en", Attributes = new() { Locale = "en" } }
            }
        };
        var diff = _sut.ComputeDiff(previous, current);
        diff.AddedLocales.Should().HaveCount(1);
        diff.AddedLocales[0].Attributes.Locale.Should().Be("en");
    }

    [Fact]
    public void ComputeDiff_RemovedLocale_DetectsRemoval()
    {
        var previous = new DatoSchema
        {
            Locales = new List<DatoLocale>
            {
                new() { Id = "en", Attributes = new() { Locale = "en" } }
            }
        };
        var current = new DatoSchema();
        var diff = _sut.ComputeDiff(previous, current);
        diff.RemovedLocales.Should().HaveCount(1);
    }

    [Fact]
    public void ComputeDiff_NewItemType_DetectsAddition()
    {
        var previous = new DatoSchema();
        var current = new DatoSchema
        {
            ItemTypes = new List<DatoItemType>
            {
                new() { Id = "1", Attributes = new() { ApiKey = "article", Name = "Article" } }
            }
        };
        var diff = _sut.ComputeDiff(previous, current);
        diff.AddedItemTypes.Should().HaveCount(1);
        diff.AddedItemTypes[0].Attributes.ApiKey.Should().Be("article");
    }

    [Fact]
    public void ComputeDiff_ModifiedItemType_DetectsModification()
    {
        var previous = new DatoSchema
        {
            ItemTypes = new List<DatoItemType>
            {
                new() { Id = "1", Attributes = new() { ApiKey = "article", Name = "Article" } }
            }
        };
        var current = new DatoSchema
        {
            ItemTypes = new List<DatoItemType>
            {
                new() { Id = "1", Attributes = new() { ApiKey = "article", Name = "Blog Post" } }
            }
        };
        var diff = _sut.ComputeDiff(previous, current);
        diff.ModifiedItemTypes.Should().HaveCount(1);
        diff.AddedItemTypes.Should().BeEmpty();
    }

    [Fact]
    public void ComputeDiff_RemovedItemType_DetectsRemoval()
    {
        var previous = new DatoSchema
        {
            ItemTypes = new List<DatoItemType>
            {
                new() { Id = "1", Attributes = new() { ApiKey = "article", Name = "Article" } }
            }
        };
        var current = new DatoSchema();
        var diff = _sut.ComputeDiff(previous, current);
        diff.RemovedItemTypes.Should().HaveCount(1);
    }

    [Fact]
    public void ComputeDiff_NewField_DetectsAddition()
    {
        var itemType = new DatoItemType { Id = "1", Attributes = new() { ApiKey = "article", Name = "Article" } };
        var previous = new DatoSchema { ItemTypes = new List<DatoItemType> { itemType } };
        var current = new DatoSchema
        {
            ItemTypes = new List<DatoItemType> { itemType },
            Fields = new List<DatoField>
            {
                new()
                {
                    Id = "f1",
                    Attributes = new() { ApiKey = "title", Label = "Title", FieldType = "string" },
                    Relationships = new() { ItemType = new() { Data = new() { Id = "1", Type = "item_type" } } }
                }
            }
        };
        var diff = _sut.ComputeDiff(previous, current);
        diff.AddedFields.Should().HaveCount(1);
        diff.AddedFields[0].Attributes.ApiKey.Should().Be("title");
    }

    [Fact]
    public void ComputeDiff_UnchangedSchema_ReturnsNoChanges()
    {
        var schema = new DatoSchema
        {
            Locales = new List<DatoLocale>
            {
                new() { Id = "en", Attributes = new() { Locale = "en" } }
            },
            ItemTypes = new List<DatoItemType>
            {
                new() { Id = "1", Attributes = new() { ApiKey = "article", Name = "Article" } }
            },
            Fields = new List<DatoField>
            {
                new()
                {
                    Id = "f1",
                    Attributes = new() { ApiKey = "title", Label = "Title", FieldType = "string" },
                    Relationships = new() { ItemType = new() { Data = new() { Id = "1", Type = "item_type" } } }
                }
            }
        };
        var diff = _sut.ComputeDiff(schema, schema);
        diff.HasChanges.Should().BeFalse();
    }
}
