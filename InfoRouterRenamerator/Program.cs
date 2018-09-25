using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using TestServiceThing;

namespace InfoRouterRenamerator
{
    class Program
    {
        const string sourceFolder = "\\NRG_DYNAMICS";

        private string _ticket;
        private srvSoapClient _client;

        static int Main(string[] args)
        {
            var p = new Program();

            var user = args[0];
            var pass = args[1];
            var localDestination = args[2];

            if (args.Length < 3 || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass) ||
                string.IsNullOrEmpty(localDestination))
                return 1;

            p.RunThing(user, pass, localDestination).GetAwaiter().GetResult();
            return 0;
        }

        public async Task RunThing(string user, string pass, string localDestinationFolder)
        {
            ConfigureClient(user, pass);
            
            var docs = await _client.GetDocumentsAsync(_ticket, sourceFolder, true, false, false, false).ConfigureAwait(false);

            var documents = docs.Elements("document");

            var targetDocs = documents.Select(OurDocument.LoadFromElement).ToList();
            await DownloadFiles(targetDocs, localDestinationFolder).ConfigureAwait(false);
            await MoveRemoteFiles(targetDocs, $"{sourceFolder}\\Processed").ConfigureAwait(false);

        }

        private void ConfigureClient(string user, string pass)
        {
            var binding = new BasicHttpsBinding {MaxReceivedMessageSize = 0x3B9ACA00};
            var endpoint = new EndpointAddress("https://scans.omnitracs.com/InfoRouter/srv.asmx");
            _client = new srvSoapClient(binding, endpoint);

            var authResp = _client.AuthenticateUserAsync(user, pass).Result;
            _ticket = authResp.Attribute("ticket").Value;
        }

        private async Task MoveRemoteFiles(IEnumerable<OurDocument> sourceDocs, string destination)
        {
            foreach (var doc in sourceDocs)
            {
                var result = await _client
                    .MoveAsync(_ticket, doc.RemoteDocumentPath, $"{destination}\\{doc.LocalFileName}")
                    .ConfigureAwait(false);
                Debug.WriteLine(result);
            }
        }

        private async Task DownloadFiles(IEnumerable<OurDocument> docs, string localDestinationFolder)
        { 
            // test first
            foreach (var doc in docs)
            {
                var file = await _client.DownloadDocumentAsync(_ticket, doc.RemoteDocumentPath);
                Debug.WriteLine($"Downloaded {doc.OriginalFileName}");
                await File
                    .WriteAllBytesAsync(Path.Combine(localDestinationFolder, doc.LocalFileName), file.DownloadDocumentResult)
                    .ConfigureAwait(false);
                Debug.WriteLine($"Saved {doc.LocalFileName}");
            }
        }
    }
}
