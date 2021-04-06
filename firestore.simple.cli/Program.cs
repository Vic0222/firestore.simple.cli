using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace firestore.simple.cli
{
    class Program
    {
        public static Task Main(string[] args)
        => CommandLineApplication.ExecuteAsync<Program>(args);


        [AllowedValues("export", "import")]
        [Argument(0)]
        [Required]
        public string Action { get; } = "export";

        [Option(ShortName = "f")]
        [Required]
        public String FilePath { get; }

        [Option(ShortName = "c")]
        [Required]
        public String Collection { get; }

        [Option(ShortName = "key", Description = "Private key from firebase. See https://firebase.google.com/docs/admin/setup/#initialize-sdk")]
        [Required]
        [FileExists]
        public String PrivateKey { get; }

        [Option(ShortName = "p")]
        [Required]
        public String ProjectId { get; }



        private async Task OnExecute()
        {
            //init firebase admin
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", PrivateKey);
            var firestore = FirestoreDb.Create(ProjectId);

            if (Action == "export")
            {
                await Export(firestore);

            }
            else if (Action == "import")
            {

                await Import(firestore);

            }
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }
        private async Task Import(FirestoreDb firestore)
        {
            if (!File.Exists(FilePath))
            {
                Console.WriteLine($"Importing failed. {FilePath} doesn't exist.");
                return;
            }

            bool continueImport = Prompt.GetYesNo("This will overwrite documents with conflicting keys. Continue?", false);
            if (!continueImport)
            {
                Console.WriteLine($"Importing cancelled");
                return;
            }

            var json = await File.ReadAllTextAsync(FilePath);

            var toImport = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string,object>>>(json);
            foreach (var item in toImport)
            {
                await firestore.Collection(Collection).Document(item.Key).SetAsync(item.Value, SetOptions.Overwrite);
            }

            Console.WriteLine($"Importing completed");
        }

        private async Task Export(FirestoreDb firestore)
        {
            Dictionary<string, object> toExport = new Dictionary<string, object>();
            Console.WriteLine("Exporting");
            var list = firestore.Collection(Collection).ListDocumentsAsync().GetAsyncEnumerator();

            while (await list.MoveNextAsync())
            {
                var item = list.Current;
                var data = await item.GetSnapshotAsync();

                toExport.Add(item.Id, data.ToDictionary());
            }

            Console.WriteLine("saving data to json file");
            var json = JsonConvert.SerializeObject(toExport);
            bool continueWrite = true;
            if (File.Exists(FilePath))
            {
                continueWrite = Prompt.GetYesNo($"{FilePath} already exist. Overwrite?", false);
            }
            if (continueWrite)
            {
                await File.WriteAllTextAsync(FilePath, json);
                Console.WriteLine("Exporing done");
            }
            else
            {
                Console.WriteLine("Exporing cancelled");
            }
        }

        public void Export(string collection, string destinationFile)
        {

        }

    }
}
