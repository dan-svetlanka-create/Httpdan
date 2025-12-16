using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;// импорт дляHttpClient
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace HttpNewsPAT
{
    internal class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient(); //используется во всей программе 

        static async Task Main(string[] args)
        {
            Trace.Listeners.Add(new TextWriterTraceListener("debug.log"));
            Trace.AutoFlush = true;
            Trace.WriteLine($"=== Запуск программы: {DateTime.Now} ===");

            Console.WriteLine("1. Добавить и парсить permaviat");
            Console.WriteLine("2. Парсить Git");
            Console.Write("Выберите: ");
            string select = Console.ReadLine();

            Trace.WriteLine($"Выбран вариант: {select}");
            if (select == "1")
            {
                Cookie token = await SingIn("user", "user"); // вызов метода SingIn (УЖЕ НА HttpClient)

                Console.WriteLine("\nДобавление новой записи");

                Console.Write("Заголовок: ");
                string name = Console.ReadLine();

                Console.Write("Текст: ");
                string description = Console.ReadLine();

                Console.Write("Ссылка на изображение: ");
                string src = Console.ReadLine();

                Trace.WriteLine($"Введены данные: name={name}, src={src}");
                bool success = await AddNews(token, name, src, description); // вызов метода  AddNews 

                if (success)
                {
                    Console.WriteLine("\nОбновлённый список:");
                    string content = await GetContent(token);
                    ParsingHtml(content);
                }
            }
            else if (select == "2")
            {
                await ParseGitHubTrending();
            }

            Trace.WriteLine($"=== Завершение программы: {DateTime.Now} ===");
            Trace.Flush();
            Console.Read();
        }

        public static async Task ParseGitHubTrending()
        {
            Trace.WriteLine("Начало парсинга GitHub Trending");

            try
            {
                string url = "https://github.com/trending";

                Console.WriteLine($"Парсим: {url}\n");

                var response = await _httpClient.GetAsync(url);
                string html = await response.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                Console.WriteLine("=== ПОПУЛЯРНЫЕ РЕПОЗИТОРИИ GITHUB ===");
                Console.WriteLine("====================================\n");

                // Ищем ВСЕ карточки репозиториев
                // У GitHub trending каждая карточка - это <article> с классом Box-row
                var repoArticles = doc.DocumentNode.SelectNodes("//article[contains(@class, 'Box-row')]");

                if (repoArticles != null && repoArticles.Count > 0)
                {
                    Console.WriteLine($"Найдено репозиториев: {repoArticles.Count}\n");

                    int count = 1;
                    foreach (var article in repoArticles.Take(6)) // Берем 6 
                    {
                        // 1. Название репозитория (внутри h3 с классом h3)
                        var titleElement = article.SelectSingleNode(".//h2");
                        string title = "";

                        if (titleElement != null)
                        {
                            // Берем весь текст из h2 и чистим его
                            title = WebUtility.HtmlDecode(titleElement.InnerText.Trim())
                                .Replace("\n", " ")          // Убираем переносы
                                .Replace("  ", " ")          // Убираем двойные пробелы
                                .Replace("  ", " ");         // Еще раз на всякий случай

                            // Убираем слово "Star" если оно есть
                            title = title.Replace("Star ", "").Replace("Unstar ", "");
                        }

                        // 2. Описание репозитория 
                        string description = "";
                        var descElement = article.SelectSingleNode(".//p");//абзац
                        if (descElement != null)
                        {
                            description = WebUtility.HtmlDecode(descElement.InnerText.Trim());
                            // Обрезаем если слишком длинное
                            if (description.Length > 70)
                                description = description.Substring(0, 70) + "...";
                        }

                        // 3. Язык программирования (опционально)
                        string language = "";
                        var langElement = article.SelectSingleNode(".//span[@itemprop='programmingLanguage']");
                        if (langElement != null)
                        {
                            language = WebUtility.HtmlDecode(langElement.InnerText.Trim());
                        }

                        // Выводим только если есть название
                        if (!string.IsNullOrEmpty(title) && title.Length > 5)
                        {
                            Console.WriteLine($"{count}. {title}");

                            if (!string.IsNullOrEmpty(description))
                                Console.WriteLine($"   {description}");

                            if (!string.IsNullOrEmpty(language))
                                Console.WriteLine($"   Язык: {language}");

                            Console.WriteLine();
                            count++;
                        }
                    }
                }
                else
                {
                    // РЕЗЕРВНЫЙ ВАРИАНТ 
                    Console.WriteLine("Использую упрощенный парсинг:\n");

                    // Ищем просто все ссылки, которые выглядят как репозитории
                    var allRepoLinks = doc.DocumentNode.SelectNodes("//a[contains(@data-hydro-click, 'repository')]");

                    if (allRepoLinks != null)
                    {
                        int simpleCount = 1;
                        var seenRepos = new HashSet<string>();

                        foreach (var link in allRepoLinks)
                        {
                            string repoText = WebUtility.HtmlDecode(link.InnerText.Trim());

                            // Фильтруем: должно быть "/" в названии (author/repo)
                            if (repoText.Contains("/") &&
                                !repoText.Contains(" ") &&
                                repoText.Length > 3 &&
                                !seenRepos.Contains(repoText))
                            {
                                Console.WriteLine($"{simpleCount}. {repoText}");
                                seenRepos.Add(repoText);
                                simpleCount++;

                                if (simpleCount > 6) break;
                            }
                        }
                    }
                    else
                    {
                        // САМЫЙ ПРОСТОЙ ВАРИАНТ - демо-данные 
                        Console.WriteLine("Показываю демонстрационные данные:\n");

                        ShowDemoRepositories();
                    }
                }

                Console.WriteLine("Источник: https://github.com/trending");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                Trace.WriteLine($"Ошибка ParseGitHubTrending: {ex.Message}");

                // Всегда показываем демо-данные при ошибке
                Console.WriteLine("\n--- Демо-данные ---\n");
                ShowDemoRepositories();
            }

            Trace.WriteLine("Конец парсинга GitHub Trending");
        }

        // Отдельный метод для демо-данных
        private static void ShowDemoRepositories()
        {
            var demoRepos = new[]
            {
        "microsoft/vscode - Редактор кода от Microsoft",
        "facebook/react - JavaScript библиотека для UI",
        "torvalds/linux - Ядро операционной системы Linux",
        "docker/compose - Инструмент для Docker-приложений",
        "nodejs/node - Среда выполнения JavaScript",
        "python/cpython - Интерпретатор Python"
            };

            for (int i = 0; i < demoRepos.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {demoRepos[i]}");
            }
        }

        public static async Task<bool> AddNews(Cookie token, string name, string src, string description)//реалтзован метод AddNews
        {
            string url = "http://10.111.20.114/ajax/add.php";

            try
            {
                Trace.WriteLine($"AddNews запрос: {url}");
                var postData = new FormUrlEncodedContent(new[]//  ИСПОЛЬЗУЕТ HttpClient ДЛЯ POST ЗАПРОСА
                {
                    new KeyValuePair<string, string>("name", name),
                    new KeyValuePair<string, string>("description", description),
                    new KeyValuePair<string, string>("src", src),
                    new KeyValuePair<string, string>("token", token.Value)
                });

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = postData;
                request.Headers.Add("Cookie", $"{token.Name}={token.Value}");

                var response = await _httpClient.SendAsync(request);
                Trace.WriteLine($"AddNews статус: {response.StatusCode}");

                string responseText = await response.Content.ReadAsStringAsync();
                Trace.WriteLine($"AddNews ответ: {responseText}");
                Console.WriteLine($"Ответ: {responseText}");

                if (response.IsSuccessStatusCode && !responseText.Contains("<html>"))
                {
                    Trace.WriteLine("Запись успешно добавлена");
                    Console.WriteLine("Запись добавлена!");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Ошибка AddNews: {ex.Message}");
                Console.WriteLine($"Ошибка: {ex.Message}");
                return false;
            }
        }
        public static void ParsingHtml(string htmlCode)
        {
            Trace.WriteLine("Начало ParsingHtml");
            var Html = new HtmlDocument();
            Html.LoadHtml(htmlCode);

            var Document = Html.DocumentNode;
            IEnumerable<HtmlNode> DivNews = Document.Descendants(0).Where(x => x.HasClass("news"));

            foreach (var DivNew in DivNews)
            {
                var src = DivNew.ChildNodes[1].GetAttributeValue("src", "node");
                var name = DivNew.ChildNodes[3].InnerHtml;
                var description = DivNew.ChildNodes[5].InnerHtml;

                Trace.WriteLine($"Парсинг: name={name}, src={src}");
                Console.WriteLine($"{name} \nИзображение: {src} \nОписание: {description}");
            }
            Trace.WriteLine("Конец ParsingHtml");
        }

        public static async Task<string> GetContent(Cookie token)  // МЕТОД GetContent ПЕРЕПИСАН НА HttpClient 
        {
            string url = "http://10.111.20.114/main";
            Trace.WriteLine($"GetContent запрос: {url}");

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (token != null)
            {
                request.Headers.Add("Cookie", $"{token.Name}={token.Value}");
            }

            try
            {
                var response = await _httpClient.SendAsync(request);
                Trace.WriteLine($"GetContent статус: {response.StatusCode}");

                return await response.Content.ReadAsStringAsync(); // ЧТЕНИЕ ОТВЕТА ЧЕРЕЗ response.Content
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Ошибка GetContent: {ex.Message}");
                Console.WriteLine($"Ошибка получения контента: {ex.Message}");
                return null;
            }
        }
        public static async Task<Cookie> SingIn(string login, string password) // МЕТОД SingIn ПЕРЕПИСАН НА HttpClient
        {
            string uri = "http://10.111.20.114/ajax/login.php";
            Trace.WriteLine($"SingIn запрос: {uri}");

            var content = new FormUrlEncodedContent(new[]  // СОЗДАНИЕ FORM-DATA ДЛЯ АВТОРИЗАЦИИ
            {
                new KeyValuePair<string, string>("login", login),
                new KeyValuePair<string, string>("password", password)
            });

            try
            {
                var response = await _httpClient.PostAsync(uri, content);
                Trace.WriteLine($"SingIn статус: {response.StatusCode}");

                string responseFromServer = await response.Content.ReadAsStringAsync();
                Trace.WriteLine($"SingIn ответ: {responseFromServer}");
                Console.WriteLine(responseFromServer);

                if (response.Headers.TryGetValues("Set-Cookie", out var cookies)) // ИЗВЛЕЧЕНИЕ TOKEN ИЗ COOKIES (КАК В POSTMAN)
                {
                    var tokenCookie = cookies.FirstOrDefault(c => c.Contains("token="));
                    if (!string.IsNullOrEmpty(tokenCookie))
                    {
                        var cookieValue = tokenCookie.Split('=')[1].Split(';')[0];
                        Trace.WriteLine($"Токен получен: {cookieValue}");
                        // ВОЗВРАЩАЕМ Cookie ДЛЯ ПОСЛЕДУЮЩИХ ЗАПРОСОВ
                        return new Cookie("token", cookieValue, "/", "10.111.20.114");
                    }
                }
                Trace.WriteLine("Токен не найден");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Ошибка SingIn: {ex.Message}");
                Console.WriteLine($"Ошибка авторизации: {ex.Message}");
            }

            return null;
        }
    }
}
