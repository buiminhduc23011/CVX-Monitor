using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace CameraSimulator;

public partial class MainWindow : Window
{
    private TcpListener? _listener;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private int _okCount = 0;
    private int _ngCount = 0;
    private bool _isRunning = false;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void StartServer_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            StopServer();
            return;
        }

        try
        {
            int port = int.Parse(PortTextBox.Text);
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _isRunning = true;
            
            StatusText.Text = $"Đang lắng nghe cổng {port}...";
            StatusIndicator.Fill = new SolidColorBrush(Colors.Orange);
            StartButton.Content = "Dừng Server";

            // Accept client connection
            _client = await _listener.AcceptTcpClientAsync();
            _stream = _client.GetStream();
            
            StatusText.Text = $"Đã kết nối từ {((IPEndPoint)_client.Client.RemoteEndPoint!).Address}";
            StatusIndicator.Fill = new SolidColorBrush(Colors.Green);
            
            OkButton.IsEnabled = true;
            NgButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}");
            StopServer();
        }
    }

    private void StopServer()
    {
        _stream?.Close();
        _client?.Close();
        _listener?.Stop();
        
        _isRunning = false;
        _okCount = 0;
        _ngCount = 0;
        
        StatusText.Text = "Đã ngắt kết nối";
        StatusIndicator.Fill = new SolidColorBrush(Colors.Red);
        StartButton.Content = "Bắt đầu Server";
        OkButton.IsEnabled = false;
        NgButton.IsEnabled = false;
        UpdateCounters();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _okCount++;
        SendPacket();
        UpdateCounters();
    }

    private void NgButton_Click(object sender, RoutedEventArgs e)
    {
        _ngCount++;
        SendPacket();
        UpdateCounters();
    }

    private void SendPacket()
    {
        if (_stream == null || !_client!.Connected) return;

        try
        {
            int total = _okCount + _ngCount;
            string productId = ProductIdTextBox.Text;
            string mfgDate = DateTime.Now.ToString("ddMMyy");
            string expDate = DateTime.Now.AddYears(1).ToString("ddMMyy");

            // Format: Total,OK,NG,ProductId,MfgDate,ExpDate
            string packet = $"{total},{_okCount},{_ngCount},{productId},{mfgDate},{expDate}";
            byte[] data = Encoding.UTF8.GetBytes(packet);
            _stream.Write(data, 0, data.Length);

            LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Sent: {packet}\n");
            LogTextBox.ScrollToEnd();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi gửi: {ex.Message}");
            StopServer();
        }
    }

    private void UpdateCounters()
    {
        OkCountText.Text = _okCount.ToString();
        NgCountText.Text = _ngCount.ToString();
        TotalCountText.Text = (_okCount + _ngCount).ToString();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _okCount = 0;
        _ngCount = 0;
        UpdateCounters();
        LogTextBox.Clear();
    }
}
