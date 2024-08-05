using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows;


public class RequestPayload
{
    public Parameters parameters { get; set; }
}

public class Parameters
{
    public int patientId { get; set; }
    public int patientIdCd { get; set; }
    public string approvalNo { get; set; }
    public string approvalCode { get; set; }
}

namespace pharma_soft_clalit_prescription
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static readonly HttpClient client = new HttpClient();
        public const string URL = "https://pharmatech.clalit.org.il:7443/Pharmatech-To-Integration-MedicalApprovalPdfQuery-Prod/ClicksMedicalApprovalPdfQuery/api/MedicalApproval/GetPdfQuery";

        private RequestPayload CreateRequestPayload(string id, string approval)
        {
            if (string.IsNullOrEmpty(id) || id.Length < 2)
            {
                throw new ArgumentException("Invalid id");
            }

            var patientIdCd = int.Parse(id[^1].ToString());
            var patientId = int.Parse(id[..^1]);

            return new RequestPayload
            {
                parameters = new Parameters
                {
                    patientId = patientId,
                    patientIdCd = patientIdCd,
                    approvalNo = approval,
                    approvalCode = "i333"
                }
            };
        }


        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
        }

        private string id;
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Id
        {
            get { return id; }
            set
            {
                id = value;
                OnPropertyChanged();
            }
        }

        private string approval;

        public string Approval
        {
            get { return approval; }
            set
            {
                approval = value;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Id) || string.IsNullOrEmpty(Approval))
            {
                MessageBox.Show("יש למלא את השדות", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var response = await SendPostRequest(Id, Approval);

                if (response.IsSuccessStatusCode)
                {
                    var fileName = await SaveFile(response,Id,Approval);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        MessageBox.Show("הקובץ נשמר בהצלחה", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        //Close();
                    }
                }
                else
                {
                    MessageBox.Show($"שגיאה: {response.ReasonPhrase}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"שגיאה: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<HttpResponseMessage> SendPostRequest(string id, string approval)
        {
            var payload = CreateRequestPayload(id, approval);

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");


            var request = new HttpRequestMessage(HttpMethod.Post, URL)
            {
                Content = content
            };


            // Set headers specific to this request
            request.Headers.Add("requestingSite", "71");
            request.Headers.Add("requestingApplication", "71");
            request.Headers.Add("requestDateTime", DateTime.UtcNow.ToString("o"));
            request.Headers.Add("requestID", "MAIN_PREPROD_12");


            var response = await client.SendAsync(request);
            return response;
        }

        private async Task<string> SaveFile(HttpResponseMessage response, string id, string approval)
        {

            string timestamp = DateTime.UtcNow.ToString("ddMMyyyy_HHmm");
            string defaultFileName = $"{id}_{approval}_{timestamp}.pdf";

            var saveFileDialog = new SaveFileDialog
            {
                FileName = defaultFileName,
                DefaultExt = ".pdf",
                Filter = "PDF documents (.pdf)|*.pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fileStream);
                fileStream.Close();
                return saveFileDialog.FileName;
            }
            return null;
        }

    }
}
