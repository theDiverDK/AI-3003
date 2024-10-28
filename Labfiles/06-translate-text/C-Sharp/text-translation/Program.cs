using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

// import namespaces
using Azure;
using Azure.AI.Translation.Text;
using System.Collections.Generic;

namespace translate_text
{
    class Program
    {
        private static string translatorEndpoint = "https://api.cognitive.microsofttranslator.com";
        private static string cogSvcKey;
        private static string cogSvcRegion;
        private static TextTranslationClient client;

        static async Task Main(string[] args)
        {
            try
            {
                // Get config settings from AppSettings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                cogSvcKey = configuration["CognitiveServiceKey"];
                cogSvcRegion = configuration["CognitiveServiceRegion"];

                // Create client using endpoint and key
                AzureKeyCredential credential = new(cogSvcKey);
                TextTranslationClient client = new(credential, cogSvcRegion);


                // Set console encoding to unicode
                Console.InputEncoding = Encoding.Unicode;
                Console.OutputEncoding = Encoding.Unicode;

                // Analyze each text file in the reviews folder
                var folderPath = Path.GetFullPath("./reviews");
                DirectoryInfo folder = new DirectoryInfo(folderPath);
                foreach (var file in folder.GetFiles("*.txt"))
                {
                    // Read the file contents
                    Console.WriteLine("\n-------------\n" + file.Name);
                    StreamReader sr = file.OpenText();
                    var text = sr.ReadToEnd();
                    sr.Close();
                    Console.WriteLine("\n" + text);

                    // Detect the language
                    string language = await GetLanguage(text);
                    Console.WriteLine("Language: " + language);

                    // Translate if not already English
                    if (language != "en")
                    {
                        string translatedText = await Translate(text, language);
                        Console.WriteLine("\nTranslation:\n" + translatedText);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static async Task<string> GetLanguage(string text)
        {
            // Default language is English
            string language = "en";

            // Choose target language
            Response<GetLanguagesResult> languagesResponse = await client.GetLanguagesAsync(scope: "translation").ConfigureAwait(false);
            GetLanguagesResult languages = languagesResponse.Value;
            Console.WriteLine($"{languages.Translation.Count} languages available.\n(See https://learn.microsoft.com/azure/ai-services/translator/language-support#translation)");
            Console.WriteLine("Enter a target language code for translation (for example, 'en'):");
            string targetLanguage = "xx";
            bool languageSupported = false;
            while (!languageSupported)
            {
                targetLanguage = Console.ReadLine();
                if (languages.Translation.ContainsKey(targetLanguage))
                {
                    languageSupported = true;
                }
                else
                {
                    Console.WriteLine($"{targetLanguage} is not a supported language.");
                }

            }


            // return the language
            return targetLanguage;
        }

        static async Task<string> Translate(string text, string sourceLanguage)
        {
            string translation = "";

            // Translate text
            string inputText = "";
            while (inputText.ToLower() != "quit")
            {
                Console.WriteLine("Enter text to translate ('quit' to exit)");
                inputText = Console.ReadLine();
                if (inputText.ToLower() != "quit")
                {
                    Response<IReadOnlyList<TranslatedTextItem>> translationResponse = await client.TranslateAsync(targetLanguage, inputText).ConfigureAwait(false);
                    IReadOnlyList<TranslatedTextItem> translations = translationResponse.Value;
                    TranslatedTextItem translation = translations[0];
                    string sourceLanguage = translation?.DetectedLanguage?.Language;
                    Console.WriteLine($"'{inputText}' translated from {sourceLanguage} to {translation?.Translations[0].To} as '{translation?.Translations?[0]?.Text}'.");
                }
            }


            // Return the translation
            return translation;

        }
    }
}

