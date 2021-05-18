using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SecretChat
{
    //Don't forget the using System.Security.Cryptography; statement when you add this class
    public static class Encrypt
    {
        // This size of the IV (in bytes) must = (keysize / 8).  Default keysize is 256, so the IV must be
        // 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.
        private const string initVector = "pemgail9uzpgzl88";
        // This constant is used to determine the keysize of the encryption algorithm
        private const int keysize = 256;
        //Encrypt
        public static string EncryptString(string plainText, string passPhrase)
        {
            try
            {
                byte[] initVectorBytes = Encoding.UTF8.GetBytes(initVector);
                byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
                byte[] keyBytes = password.GetBytes(keysize / 8);
                RijndaelManaged symmetricKey = new RijndaelManaged
                {
                    Mode = CipherMode.CBC
                };
                ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);
                MemoryStream memoryStream = new MemoryStream();
                CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                cryptoStream.FlushFinalBlock();
                byte[] cipherTextBytes = memoryStream.ToArray();
                memoryStream.Close();
                cryptoStream.Close();
                return Convert.ToBase64String(cipherTextBytes);
            }
            catch
            {
                return plainText;
            }
        }
        //Decrypt
        public static string DecryptString(string cipherText, string passPhrase)
        {
            try
            {
                byte[] initVectorBytes = Encoding.UTF8.GetBytes(initVector);
                byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
                PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null);
                byte[] keyBytes = password.GetBytes(keysize / 8);
                RijndaelManaged symmetricKey = new RijndaelManaged
                {
                    Mode = CipherMode.CBC
                };
                ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);
                MemoryStream memoryStream = new MemoryStream(cipherTextBytes);
                CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                memoryStream.Close();
                cryptoStream.Close();
                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
            }
            catch
            {
                return cipherText;
            }
        }


        //public static string EncryptText(string publicKey, string text)
        //{
        //    // Convert the text to an array of bytes   
        //    UnicodeEncoding byteConverter = new UnicodeEncoding();
        //    byte[] dataToEncrypt = byteConverter.GetBytes(text);

        //    // Create a byte array to store the encrypted data in it   
        //    byte[] encryptedData;
        //    using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
        //    {
        //        // Set the rsa pulic key   
        //        rsa.FromXmlString(publicKey);

        //        // Encrypt the data and store it in the encyptedData Array   
        //        encryptedData = rsa.Encrypt(dataToEncrypt, false);
        //    }
        //    // Save the encypted data array into a file   
        //    return byteConverter.GetString(encryptedData);
        //    //Console.WriteLine("Data has been encrypted");
        //}

        //// Method to decrypt the data withing a specific file using a RSA algorithm private key   
        //public static string DecryptData(string privateKey, string encryptedText)
        //{
        //    // read the encrypted bytes from the file   
        //    UnicodeEncoding byteConverter = new UnicodeEncoding();
        //    var dataToDecrypt = byteConverter.GetBytes(encryptedText);
        //    // Create an array to store the decrypted data in it   
        //    byte[] decryptedData;
        //    using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
        //    {
        //        // Set the private key of the algorithm   
        //        rsa.FromXmlString(privateKey);
        //        decryptedData = rsa.Decrypt(dataToDecrypt, false);
        //    }

        //    // Get the string value from the decryptedData byte array   
        //    return byteConverter.GetString(decryptedData);
        //}
    }
}
