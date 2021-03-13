using System.IO;
using System.Security.Cryptography;

namespace ArrArchiverLib.Extensions
{
    public static class AesManagedExtensions
    {
        public static AesManaged Initialize(this AesManaged aesManaged, string encryptKey, byte[] salt= null)
        {
            aesManaged.KeySize = 256;
            aesManaged.BlockSize = 128;
            
            if (salt != null)
            {
                aesManaged.IV = salt;
            }

            var keyGenerator = new Rfc2898DeriveBytes(encryptKey, aesManaged.IV);
            aesManaged.Key = keyGenerator.GetBytes(aesManaged.KeySize / 8);
            
            return aesManaged;
        }
        
    }
}