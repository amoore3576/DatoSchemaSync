using System.Text.Json.Serialization;

namespace DatoSchemaSync.Models;

public class DatoSchema
{
    public List<DatoLocale> Locales { get; set; } = new();
    public List<DatoItemType> ItemTypes { get; set; } = new();
    public List<DatoField> Fields { get; set; } = new();
}

public class DatoLocale
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("type")]
    public string Type { get; set; } = "site_locale";
    [JsonPropertyName("attributes")]
    public DatoLocaleAttributes Attributes { get; set; } = new();
}

public class DatoLocaleAttributes
{
    [JsonPropertyName("locale")]
    public string Locale { get; set; } = string.Empty;
}

public class DatoItemType
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("type")]
    public string Type { get; set; } = "item_type";
    [JsonPropertyName("attributes")]
    public DatoItemTypeAttributes Attributes { get; set; } = new();
    [JsonPropertyName("relationships")]
    public DatoItemTypeRelationships? Relationships { get; set; }
}

public class DatoItemTypeAttributes
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("api_key")]
    public string ApiKey { get; set; } = string.Empty;
    [JsonPropertyName("singleton")]
    public bool Singleton { get; set; }
    [JsonPropertyName("sortable")]
    public bool Sortable { get; set; }
    [JsonPropertyName("modular_block")]
    public bool ModularBlock { get; set; }
    [JsonPropertyName("tree")]
    public bool Tree { get; set; }
    [JsonPropertyName("ordering_direction")]
    public string? OrderingDirection { get; set; }
    [JsonPropertyName("draft_mode_active")]
    public bool DraftModeActive { get; set; }
    [JsonPropertyName("all_locales_required")]
    public bool AllLocalesRequired { get; set; }
    [JsonPropertyName("collection_appearance")]
    public string? CollectionAppearance { get; set; }
}

public class DatoItemTypeRelationships
{
    [JsonPropertyName("ordering_field")]
    public DatoRelationshipData? OrderingField { get; set; }
    [JsonPropertyName("title_field")]
    public DatoRelationshipData? TitleField { get; set; }
}

public class DatoRelationshipData
{
    [JsonPropertyName("data")]
    public DatoIdRef? Data { get; set; }
}

public class DatoIdRef
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class DatoField
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("type")]
    public string Type { get; set; } = "field";
    [JsonPropertyName("attributes")]
    public DatoFieldAttributes Attributes { get; set; } = new();
    [JsonPropertyName("relationships")]
    public DatoFieldRelationships? Relationships { get; set; }
}

public class DatoFieldAttributes
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;
    [JsonPropertyName("field_type")]
    public string FieldType { get; set; } = string.Empty;
    [JsonPropertyName("api_key")]
    public string ApiKey { get; set; } = string.Empty;
    [JsonPropertyName("hint")]
    public string? Hint { get; set; }
    [JsonPropertyName("localized")]
    public bool Localized { get; set; }
    [JsonPropertyName("validators")]
    public System.Text.Json.JsonElement? Validators { get; set; }
    [JsonPropertyName("appearance")]
    public System.Text.Json.JsonElement? Appearance { get; set; }
    [JsonPropertyName("position")]
    public int Position { get; set; }
    [JsonPropertyName("default_value")]
    public System.Text.Json.JsonElement? DefaultValue { get; set; }
}

public class DatoFieldRelationships
{
    [JsonPropertyName("item_type")]
    public DatoRelationshipData? ItemType { get; set; }
    [JsonPropertyName("fieldset")]
    public DatoRelationshipData? Fieldset { get; set; }
}
