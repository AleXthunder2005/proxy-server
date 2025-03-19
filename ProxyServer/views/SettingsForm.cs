using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ProxyServer.models.Blocker;

namespace ProxyServer.views
{
    public partial class SettingsForm: Form
    {
        public SettingsForm()
        {
            InitializeComponent();
            string[] sites = LoadBlockedSitesFromFile();
            foreach (string site in sites)
            {
                tbBlackList.Text += $"{site}\r\n";
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            try
            {
                string blackListText = tbBlackList.Text;
                string[] lines = blackListText.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                string path = "blacklist.txt";
                File.WriteAllLines(path, lines);
                UpdateBlackList(lines);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                // Обработка ошибок (например, если нет прав на запись в файл)
                MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string[] LoadBlockedSitesFromFile(string path = "blacklist.txt")
        {
            try
            {
                if (File.Exists(path))
                {
                    // Читаем все строки из файла
                    var lines = File.ReadAllLines(path);

                    // Фильтруем строки: удаляем пустые строки
                    var filteredLines = lines
                        .Select(line => line.Trim()) // Убираем пробелы в начале и конце
                        .Where(line => !string.IsNullOrEmpty(line)) // Игнорируем пустые строки
                        .ToArray(); // Преобразуем в массив

                    return filteredLines;
                }
                else
                {
                    return Array.Empty<string>();
                }
            }
            catch (Exception ex)
            {
                return Array.Empty<string>();
            }
        }
    }
}
