/**
/// ScrimpNet.Core Library
/// Copyright © 2005-2011
///
/// This module is Copyright © 2005-2011 Steve Powell
/// All rights reserved.
///
/// This library is free software; you can redistribute it and/or
/// modify it under the terms of the Microsoft Public License (Ms-PL)
/// 
/// This library is distributed in the hope that it will be
/// useful, but WITHOUT ANY WARRANTY; without even the implied
/// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
/// PURPOSE.  See theMicrosoft Public License (Ms-PL) License for more
/// details.
///
/// You should have received a copy of the Microsoft Public License (Ms-PL)
/// License along with this library; if not you may 
/// find it here: http://www.opensource.org/licenses/ms-pl.html
///
/// Steve Powell, spowell@scrimpnet.com
**/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrimpNet.Web;
using ScrimpNet;
using System.Security.Cryptography;

namespace ScrimpNet.Cryptography
{
    public partial class CryptoUtils
    {
        /// <summary>
        /// Miscellaneous helper methods to generate passwords, byte lists, etc. and whose methods do not easily fall into some other class
        /// </summary>
        public class Generate
        {
            /// <summary>
            /// Generate a random length of bytes suitable for cryptographical purposes.  Set <paramref name="minLength"/> and <paramref name="maxLength"/> to the same value for a fixed size.
            /// </summary>
            /// <param name="minLength">Shortest possible length of generated list</param>
            /// <param name="maxLength">Longest possible length of genreated list</param>
            /// <returns>Random bytes with length randomly chosen between <paramref name="minLength"/> and <paramref name="maxLength"/> inclusive.</returns>
            public static byte[] RandomBytes(int minLength, int maxLength)
            {
                int totalLength = maxLength;
                int difference = maxLength - minLength;
                Random rdm = new Random();
                if (difference > 2)
                {
                    totalLength = rdm.Next(minLength, maxLength - 1);
                }

                byte[] retval = new byte[totalLength];
                using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
                {
                    rngCsp.GetBytes(retval);
                }
                return retval;
            }

            /// <summary>
            /// Generate a random length of bytes suitable for cryptographical purposes.  Set <paramref name="minLength"/> and <paramref name="maxLength"/> to the same value for a fixed size.
            /// </summary>
            /// <param name="minLength">Shortest possible length of generated list</param>
            /// <param name="maxLength">Longest possible length of genreated list</param>
            /// <returns>Random bytes with length randomly chosen between <paramref name="minLength"/> and <paramref name="maxLength"/> inclusive. The returned value can be pasted into C# code.</returns>
            public static string RandomBytesCS(int minLength, int maxLength)
            {
                byte[] salt = RandomBytes(minLength, maxLength);
                StringBuilder sb = new StringBuilder();
                sb.Append("new byte[] {");
                foreach (byte b in salt)
                {
                    sb.Append("{0},", (int)b);
                }
                sb.Append("};");
                return sb.ToString();
            }

            private static char[] punctuations = "!@#$%^&*()_-+=[{]};:>|./?".ToCharArray();
            /// <summary>
            /// Generate a random password of a given length
            /// </summary>
            /// <param name="length">Total length of password to generate</param>
            /// <param name="numberOfNonAlphanumericCharacters">Number of non-alpha numeric characters to use</param>
            /// <returns>Generated password</returns>
            [Citation(".Net Framework Library: System.Web.Security")]
            public static string Password(int length, int numberOfNonAlphanumericCharacters)
            {
                string str;
                int num;

                do
                {
                    byte[] data = new byte[length];
                    char[] chArray = new char[length];
                    int num2 = 0;
                    new RNGCryptoServiceProvider().GetBytes(data);
                    for (int i = 0; i < length; i++)
                    {
                        int num4 = data[i] % 0x57;
                        if (num4 < 10)
                        {
                            chArray[i] = (char)(0x30 + num4);
                        }
                        else if (num4 < 0x24)
                        {
                            chArray[i] = (char)((0x41 + num4) - 10);
                        }
                        else if (num4 < 0x3e)
                        {
                            chArray[i] = (char)((0x61 + num4) - 0x24);
                        }
                        else
                        {
                            chArray[i] = punctuations[num4 - 0x3e];
                            num2++;
                        }
                    }
                    if (num2 < numberOfNonAlphanumericCharacters)
                    {
                        Random random = new Random();
                        for (int j = 0; j < (numberOfNonAlphanumericCharacters - num2); j++)
                        {
                            int num6;
                            do
                            {
                                num6 = random.Next(0, length);
                            }
                            while (!char.IsLetterOrDigit(chArray[num6]));
                            chArray[num6] = punctuations[random.Next(0, punctuations.Length)];
                        }
                    }
                    str = new string(chArray);
                }
                while (CrossSiteScriptingValidation.IsDangerousString(str, out num));
                return str;
            }

        }
    }
}
