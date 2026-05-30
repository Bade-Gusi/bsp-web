namespace BeiShui.ApiGateway.Models;

public class Holiday
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Date { get; set; } = "";
    public string Message { get; set; } = "";
    public string Color1 { get; set; } = "#4ADE80";
    public string Color2 { get; set; } = "#2DD4BF";
    public string Emoji { get; set; } = "🎉";
    public string ImageUrl { get; set; } = "";
    public int Type { get; set; }
    public bool IsActive { get; set; } = true;
}

public class WelfareItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Link { get; set; } = "";
    public string Color1 { get; set; } = "#4ADE80";
    public string Color2 { get; set; } = "#2DD4BF";
    public string Icon { get; set; } = "❤️";
    public bool IsActive { get; set; } = true;
}
