﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net;
using System.Net.Sockets;
namespace Websocket
{
    public class Command
    {
        public string command { get; set; }
        public string args { get; set; }
    }

    public class Server
    {
        private byte[] buffer;
        public Server()
        {
            this.buffer = new byte[4096];
        }

        public bool Negotiate(NetworkStream networkStream)
        {

            var i = 0;
            try
            {
                i = networkStream.Read(buffer, 0, buffer.Length);
            }
            catch
            {
                return false;
            }
            if (i <= 0)
                return false;
            string headerResponse = (System.Text.Encoding.UTF8.GetString(buffer)).Substring(0, i);                        
            var key = headerResponse.Replace("ey:", "`").Split('`')[1].Replace("\r", "").Split('\n')[0].Trim();
            var test1 = Websocket.Util.AcceptKey(ref key);
            /*if (keyhash != "ysnV8aP1xDNN6JTuW46+oBqRErY=")
            {
                Console.WriteLine("key: " + keyhash);
                return false;
            }*/

            var newLine = "\r\n";

            var response = "HTTP/1.1 101 Switching Protocols" + newLine
                  + "Upgrade: websocket" + newLine
                  + "Connection: Upgrade" + newLine
                  + "Sec-WebSocket-Accept: " + test1 + newLine + newLine
                //+ "Sec-WebSocket-Protocol: chat, superchat" + newLine
                //+ "Sec-WebSocket-Version: 13" + newLine
                  ;
            var sendBytes = System.Text.Encoding.UTF8.GetBytes(response);
            try
            {
                networkStream.Write(sendBytes, 0, sendBytes.Length);
            }
            catch
            {

            }
            return false;            
        }

        public string GetMessage(NetworkStream networkStream)
        {
            int i;
            try
            {
                
                i = networkStream.Read(this.buffer, 0, buffer.Length);                                      
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return "";
            }
            Console.WriteLine("data Length: " + i);
            Console.WriteLine("Can timeout: " + networkStream.CanTimeout.ToString());
            string message;
            if (i == 0)
            {
                    return "zero";                
            }
            var opCode = (buffer[0] & 0xf);
            Console.WriteLine("opCode: " + opCode.ToString());
            if (opCode == 8)
                return "timeout";

            try
            {
                message = Websocket.Util.GetDecodedData(buffer, i);
            }
            catch (Exception e)
            {
                return "";                
            }
            return message; 
        }
    }
    
    public static class Util
    {
        static private string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        static SHA1 sha1 = SHA1CryptoServiceProvider.Create();
        public static T[] SubArray<T>(T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }      

        public static string AcceptKey(ref string key)
        {
            string longKey = key + guid;
            byte[] hashBytes = ComputeHash(longKey);
            return Convert.ToBase64String(hashBytes);
        }

        
        private static byte[] ComputeHash(string str)
        {
            return sha1.ComputeHash(System.Text.Encoding.ASCII.GetBytes(str));
        }

        //Needed to decode frame
        public static string GetDecodedData(byte[] buffer, int length)
        {
            byte b = buffer[1];
            
            int dataLength = 0;
            int totalLength = 0;
            int keyIndex = 0;

            if (b - 128 <= 125)
            {
                dataLength = b - 128;
                keyIndex = 2;
                totalLength = dataLength + 6;
            }

            if (b - 128 == 126)
            {
                dataLength = BitConverter.ToInt16(new byte[] { buffer[3], buffer[2] }, 0);
                keyIndex = 4;
                totalLength = dataLength + 8;
            }

            if (b - 128 == 127)
            {
                dataLength = (int)BitConverter.ToInt64(new byte[] { buffer[9], buffer[8], buffer[7], buffer[6], buffer[5], buffer[4], buffer[3], buffer[2] }, 0);
                keyIndex = 10;
                totalLength = dataLength + 14;
            }

            if (totalLength > length)
                throw new Exception("The buffer length is smaller than the data length");

            byte[] key = new byte[] { buffer[keyIndex], buffer[keyIndex + 1], buffer[keyIndex + 2], buffer[keyIndex + 3] };

            int dataIndex = keyIndex + 4;
            int count = 0;
            for (int i = dataIndex; i < totalLength; i++)
            {
                buffer[i] = (byte)(buffer[i] ^ key[count % 4]);
                count++;
            }

            return Encoding.ASCII.GetString(buffer, dataIndex, dataLength);
        }

        //function to create  frames to send to client 
        /// <summary>
        /// Enum for opcode types
        /// </summary>
        public enum EOpcodeType
        {
            /* Denotes a continuation code */
            Fragment = 0,

            /* Denotes a text code */
            Text = 1,

            /* Denotes a binary code */
            Binary = 2,

            /* Denotes a closed connection */
            ClosedConnection = 8,

            /* Denotes a ping*/
            Ping = 9,

            /* Denotes a pong */
            Pong = 10
        }

        /// <summary>Gets an encoded websocket frame to send to a client from a string</summary>
        /// <param name="Message">The message to encode into the frame</param>
        /// <param name="Opcode">The opcode of the frame</param>
        /// <returns>Byte array in form of a websocket frame</returns>
        public static byte[] GetFrameFromString(string Message, EOpcodeType Opcode = EOpcodeType.Text)
        {
            byte[] response;
            byte[] bytesRaw = Encoding.Default.GetBytes(Message);
            byte[] frame = new byte[10];

            int indexStartRawData = -1;
            int length = bytesRaw.Length;

            frame[0] = (byte)(128 + (int)Opcode);
            if (length <= 125)
            {
                frame[1] = (byte)length;
                indexStartRawData = 2;
            }
            else if (length >= 126 && length <= 65535)
            {
                frame[1] = (byte)126;
                frame[2] = (byte)((length >> 8) & 255);
                frame[3] = (byte)(length & 255);
                indexStartRawData = 4;
            }
            else
            {
                frame[1] = (byte)127;
                frame[2] = (byte)((length >> 56) & 255);
                frame[3] = (byte)((length >> 48) & 255);
                frame[4] = (byte)((length >> 40) & 255);
                frame[5] = (byte)((length >> 32) & 255);
                frame[6] = (byte)((length >> 24) & 255);
                frame[7] = (byte)((length >> 16) & 255);
                frame[8] = (byte)((length >> 8) & 255);
                frame[9] = (byte)(length & 255);

                indexStartRawData = 10;
            }

            response = new byte[indexStartRawData + length];

            int i, reponseIdx = 0;

            //Add the frame bytes to the reponse
            for (i = 0; i < indexStartRawData; i++)
            {
                response[reponseIdx] = frame[i];
                reponseIdx++;
            }

            //Add the data bytes to the response
            for (i = 0; i < length; i++)
            {
                response[reponseIdx] = bytesRaw[i];
                reponseIdx++;
            }

            return response;
        }

    }
}
