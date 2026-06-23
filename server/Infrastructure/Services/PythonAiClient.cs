using Application.Common.DTOs.Ai;
using Application.Features.Ai;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class PythonAiClient : IAiClient
    {
        private readonly HttpClient _httpClient;

        public PythonAiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<TaskRiskAiResponse> PredictRiskAsync(
            TaskRiskAiRequest request,
            CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/predict-risk",
                request,
                cancellationToken);

            return await ReadResponseAsync<TaskRiskAiResponse>(response, cancellationToken);
        }

        public async Task<StaffMatchAiResponse> MatchStaffAsync(
            StaffMatchAiRequest request,
            CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/match-staff",
                request,
                cancellationToken);

            return await ReadResponseAsync<StaffMatchAiResponse>(response, cancellationToken);
        }

        public async Task<IReadOnlyList<BottleneckAiResponse>> AnalyzeBottleneckAsync(
            int topN,
            CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync(
                $"/api/analyze-bottleneck?top_n={topN}",
                cancellationToken);

            return await ReadResponseAsync<List<BottleneckAiResponse>>(response, cancellationToken);
        }

        private static async Task<T> ReadResponseAsync<T>(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"AI server error {(int)response.StatusCode}: {errorBody}");
            }

            var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);

            if (result == null)
            {
                throw new HttpRequestException("AI server returned empty response.");
            }

            return result;
        }
    }
}
