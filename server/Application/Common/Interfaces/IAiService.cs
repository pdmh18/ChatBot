namespace Application.Common.Interfaces;

public interface IAiService
{
    Task<string> ChatAsync(string userMessage, List<(string Role, string Content)> history);
    Task<object> AnalyzeProjectRiskAsync(object projectData);
}
