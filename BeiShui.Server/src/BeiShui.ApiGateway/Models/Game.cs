namespace BeiShui.ApiGateway.Models;

public class Game
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ShortName { get; set; } = "";
    public string ProcessName { get; set; } = "";
    public string LauncherArgs { get; set; } = "";
    public string CoverUrl { get; set; } = "";
    public int Status { get; set; } = 1; // 0=下架 1=在线
}
