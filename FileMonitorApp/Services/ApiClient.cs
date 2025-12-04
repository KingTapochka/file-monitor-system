using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using FileMonitorApp.Models;
using Newtonsoft.Json;

namespace FileMonitorApp.Services
{
    /// <summary>
    /// HTTP клиент для взаимодействия с FileMonitorService API
    /// </summary>
    public class ApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ApiClient() : this(ConfigManager.ServerAddress)
        {
        }

        public ApiClient(string serverAddress)
        {
            _baseUrl = serverAddress.TrimEnd('/');
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        /// <summary>
        /// Получить список пользователей, которые открыли файл
        /// </summary>
        public async Task<List<FileUserInfo>?> GetFileUsersAsync(string filePath)
        {
            try
            {
                // Конвертируем локальный путь в серверный, если необходимо
                var serverPath = ConvertToServerPath(filePath);
                var encodedPath = HttpUtility.UrlEncode(serverPath);
                var url = $"{_baseUrl}/api/files/users?filePath={encodedPath}";

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<FileUsersResponse>(json);
                    return result?.Users != null 
                        ? new List<FileUserInfo>(result.Users) 
                        : new List<FileUserInfo>();
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Файл не открыт - возвращаем пустой результат
                    return new List<FileUserInfo>();
                }

                throw new Exception($"Сервер вернул ошибку: {(int)response.StatusCode} {response.StatusCode}");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Не удается подключиться к серверу: {ex.Message}", ex);
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Превышено время ожидания ответа от сервера");
            }
        }

        /// <summary>
        /// Получить список активных файлов
        /// </summary>
        public async Task<ActiveFilesResponse?> GetActiveFilesAsync()
        {
            try
            {
                var url = $"{_baseUrl}/api/files/active";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<ActiveFilesResponse>(json);
                }

                throw new Exception($"Сервер вернул ошибку: {(int)response.StatusCode}");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Не удается подключиться к серверу: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Проверка работоспособности сервиса
        /// </summary>
        public async Task<(bool Success, string Message)> CheckHealthAsync()
        {
            try
            {
                var url = $"{_baseUrl}/api/files/health";
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Подключение успешно!");
                }
                else
                {
                    return (false, $"Сервер вернул ошибку: {(int)response.StatusCode} {response.ReasonPhrase}");
                }
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Не удается подключиться: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return (false, "Превышено время ожидания (10 сек)");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Конвертирует сетевой путь (\\server\share\file) в локальный путь на сервере
        /// </summary>
        private string ConvertToServerPath(string networkPath)
        {
            // Если это уже локальный путь (C:\...), возвращаем как есть
            if (networkPath.Length >= 2 && networkPath[1] == ':')
            {
                return networkPath;
            }

            // Если это UNC путь (\\server\share\...), пытаемся преобразовать
            // \\server\sharename\folder\file.txt -> D:\SharedFolder\folder\file.txt
            // Это преобразование должно быть настроено на стороне сервера
            // Пока просто возвращаем путь как есть
            return networkPath;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
