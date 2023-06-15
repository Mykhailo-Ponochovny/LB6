﻿using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace CSharp_LB6
{
    public class Functions
    {
        private const string _serverIP = "192.168.31.202";

        public static string GetUserNameFromDialog()
        {
            var userNameDialog = new UserNameDialog();
            userNameDialog.ShowDialog();
            var userName = userNameDialog.userName;
            
            SerializeJson(userName, "username.json");
            
            return userName;
        }
        
        public static string GetUserName(List<string> usersName)
        {
            string userName;
            
            if (!File.Exists("data/username.json"))
            {
                
                while (true)
                {
                    string tempUserName = Functions.GetUserNameFromDialog();
                    bool check = Functions.CheckNameRepeat(usersName, tempUserName);

                    if (check)
                        MessageBox.Show("Це ім'я вже використовується! Оберіть інше!", "Error!", MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    else
                    {
                        userName = tempUserName;
                        break;
                    }
                }
                SerializeJson(userName, "username.json");
            }
            else
            {
                var readUserNameJsonFile = File.ReadAllText("data/username.json");
                userName = JsonSerializer.Deserialize<string>(readUserNameJsonFile);
            }

            return userName;
        }
        
        private static void SerializeJson(string text, string fileName)
        {
            var saveJsonFile = JsonSerializer.Serialize(text);
            if (!Directory.Exists("data"))
                Directory.CreateDirectory("data");
            File.WriteAllText("data/" + fileName, saveJsonFile);
        }

        public static UserFile SelectFile(bool openDialogAboutFile, List<UserFile> userFiles)
        {
            var newFile = new UserFile();
            var openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "c:\\";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var fInfo = new FileInfo(openFileDialog.FileName);
                newFile.name = fInfo.Name;
                newFile.fileWeight = fInfo.Length;
                newFile.path = fInfo.DirectoryName;
                newFile.createDate = fInfo.CreationTime;

                var findUserFile = userFiles.Find(x => x.name.Equals(newFile.name) && x.path.Equals(newFile.path));
                if (findUserFile == null)
                {
                    //open save file
                    if (openDialogAboutFile)
                    {
                        var dialogAboutFile = new DialogAboutFile(newFile, userFiles);
                        dialogAboutFile.ShowDialog();
                        newFile = dialogAboutFile.userFile;
                    }
                }
                else
                {
                    MessageBox.Show("Цей файл вже доданий до списку!", "Error!", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    newFile = new UserFile();
                }
            }

            return newFile;
        }

        internal static void UpdatePersonalDataGridView(DataGridView dataGridView, List<UserFile> userFiles)
        {
            dataGridView.Rows.Clear();
            for (var i = 0; i < userFiles.Count; i++)
            {
                string sighn;
                if (userFiles[i].isAvailable)
                    sighn = "+";
                else
                    sighn = "-";
                dataGridView.Rows.Add(i + 1, userFiles[i].name, userFiles[i].fileWeight / 1000000 + " мб.",
                    userFiles[i].path, userFiles[i].createDate, sighn);
            }
        }
        
        internal static void UpdateOtherDataGridView(DataGridView dataGridView, List<UserFile> userFiles)
        {
            dataGridView.Rows.Clear();
            bool checkOutput = false;
            if (userFiles.Count == 0)
                MessageBox.Show("В цього користувача відсутня інформація про файли!", "Information!",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
            {
                int c = 1;
                foreach (var info in userFiles)
                {
                    string sighn;
                    if (!info.isAvailable) continue;
                    checkOutput = true;
                    sighn = "+";
                    dataGridView.Rows.Add(c, info.name, info.fileWeight / 1000000 + " мб.",
                        info.path, info.createDate, sighn);
                    c++;
                }
            }
            if (!checkOutput && userFiles.Count > 0)
                MessageBox.Show("В цього користувача відсутня інформація про файли!", "Information!",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void SerializeXmlUserData(List<UserFile> userFiles, string userName)
        {
            var xmlSerializer = new XmlSerializer(typeof(List<UserFile>));

            using (var fs = new FileStream("data/" + userName + "UserData.xml", FileMode.Create))
                xmlSerializer.Serialize(fs, userFiles);
        }

        public static List<UserFile> DeserializeXmlUserData(string userName)
        {
            var xmlSerializer = new XmlSerializer(typeof(List<UserFile>));
            
            using (var fs = new FileStream("data/" + userName + "UserData.xml", FileMode.Open))
            {
                var deserializeUserFiles = (List<UserFile>)xmlSerializer.Deserialize(fs);
                return deserializeUserFiles;
            }
        }

        public static List<string> DeserializeXmlUsersName()
        {
            var xmlSerializer = new XmlSerializer(typeof(List<string>));
            
            using (var fs = new FileStream("data/Users.xml", FileMode.Open))
            {
                var deserializeUsersName = (List<string>)xmlSerializer.Deserialize(fs);
                return deserializeUsersName;
            }
        }

        internal static string LinkToServer()
        {
            string result;
            try
            {
                var client = new TcpClient(_serverIP, 1111);
                result = "Online";
            }
            catch
            {
                result = "Offline";
            }

            return result;
        }

        internal static void SendFileToServer(string userName)
        {
            var client = new TcpClient(_serverIP, 1111);

            var stream = client.GetStream();
            
            byte[] requestBuffer = Encoding.ASCII.GetBytes("send");
            stream.Write(requestBuffer, 0, requestBuffer.Length);
            Thread.Sleep(1000);
            
            // Send the file name to the server
            string fileName = Path.GetFileName("data/" + userName + "UserData.xml");
            byte[] fileNameBuffer = Encoding.ASCII.GetBytes(fileName);
            stream.Write(fileNameBuffer, 0, fileNameBuffer.Length);
            Thread.Sleep(1000);
            // Send the file content to the server
            using (FileStream fileStream = File.OpenRead("data/" + userName + "UserData.xml")) {
                byte[] buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    stream.Write(buffer, 0, bytesRead);
            }

            client.Close();
        }

        internal static void ChangeFiles(string oldName, string userName, List<UserFile> userFiles)
        {
            File.Delete("data/" + oldName + "UserData.xml");
            
            var client = new TcpClient(_serverIP, 1111);

            var stream = client.GetStream();
            
            byte[] requestBuffer = Encoding.ASCII.GetBytes("remove file");
            stream.Write(requestBuffer, 0, requestBuffer.Length);
            Thread.Sleep(50);
            
            // Send the file name to the server
            string fileName = oldName + "UserData.xml";
            byte[] fileNameBuffer = Encoding.ASCII.GetBytes(fileName);
            stream.Write(fileNameBuffer, 0, fileNameBuffer.Length);
            client.Close();
            
            //sendNew UserData file
            SerializeXmlUserData(userFiles, userName);
            SendFileToServer(userName);
        }

        internal static List<string> GetOtherUsersName()
        {
            TcpClient client = new TcpClient(_serverIP, 1111);

            NetworkStream stream = client.GetStream();
            
            byte[] requestBuffer = Encoding.ASCII.GetBytes("give usernames");
            stream.Write(requestBuffer, 0, requestBuffer.Length);
            Thread.Sleep(1000);
            
            byte[] fileNameBuffer = new byte[1024];
            int responseRead = stream.Read(fileNameBuffer, 0, fileNameBuffer.Length);
            string responseText = Encoding.ASCII.GetString(fileNameBuffer, 0, responseRead);
            if (responseText == "Users not found")
                MessageBox.Show("Users not found!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
            {
                // Receive the file content from the server and save it to a file
                SaveFile(responseText, "Users.xml");

                return DeserializeXmlUsersName();
            }

            return new List<string>();
        }

        internal static List<UserFile> GetUserXmlFile(string userName)
        {
            string fileName = userName + "UserData.xml";
            
            var client = new TcpClient(_serverIP, 1111);
            var stream = client.GetStream();
            
            //send request to server
            string request = "get";
            byte[] requestBuffer = Encoding.ASCII.GetBytes(request);
            stream.Write(requestBuffer, 0, requestBuffer.Length);
            Thread.Sleep(1000);
            
            //send fileName to server
            byte[] fileNameBuffer = Encoding.ASCII.GetBytes(fileName);
            stream.Write(fileNameBuffer, 0, fileNameBuffer.Length);

            // Receive the response from the server
            byte[] responseBuffer = new byte[1024];
            int responseBytesRead = stream.Read(responseBuffer, 0, responseBuffer.Length);
            string response = Encoding.ASCII.GetString(responseBuffer, 0, responseBytesRead);

            // Receive the file content from the server and save it to a file
            if (response != "File not found")
                SaveFile(response, fileName);
            else
                MessageBox.Show("Інформація про цього користувача відсутня!", "Error!", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            return DeserializeXmlUserData(userName);
        }

        private static void SaveFile(string fileContent, string fileName) 
        {
            string dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "data");

            // Create the "data" directory if it doesn't exist
            if (!Directory.Exists(dataDirectory)) 
                Directory.CreateDirectory(dataDirectory);

            // Save the file to the "data" directory
            string filePath = Path.Combine(dataDirectory, fileName);
            File.WriteAllText(filePath, fileContent);
        }

        internal static bool CheckNameRepeat(List<string> otherUsersName, string userName)
        {
            bool result = false;

            var findUserName = otherUsersName.Find(un => un.Equals(userName));

            if (findUserName != null)
                result = true;

            return result;
        }

        internal static void RemoveUserInfo(string userName)
        {
            string fileName = userName + "UserData.xml";
            
            var client = new TcpClient(_serverIP, 1111);
            var stream = client.GetStream();
            
            //send request to server
            string request = "remove file";
            byte[] requestBuffer = Encoding.ASCII.GetBytes(request);
            stream.Write(requestBuffer, 0, requestBuffer.Length);
            Thread.Sleep(1000);
            
            //send fileName to server
            byte[] fileNameBuffer = Encoding.ASCII.GetBytes(fileName);
            stream.Write(fileNameBuffer, 0, fileNameBuffer.Length);
            
            Directory.Delete("data", true);
        }
    }
}
