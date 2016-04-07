﻿// Written by Joe Zachary for CS 3500, November 2012
// Revised by Joe Zachary April 2016

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace CustomNetworking
{
    /// <summary> 
    /// A StringSocket is a wrapper around a Socket.  It provides methods that
    /// asynchronously read lines of text (strings terminated by newlines) and 
    /// write strings. (As opposed to Sockets, which read and write raw bytes.)  
    ///
    /// StringSockets are thread safe.  This means that two or more threads may
    /// invoke methods on a shared StringSocket without restriction.  The
    /// StringSocket takes care of the synchronization.
    /// 
    /// Each StringSocket contains a Socket object that is provided by the client.  
    /// A StringSocket will work properly only if the client refrains from calling
    /// the contained Socket's read and write methods.
    /// 
    /// If we have an open Socket s, we can create a StringSocket by doing
    /// 
    ///    StringSocket ss = new StringSocket(s, new UTF8Encoding());
    /// 
    /// We can write a string to the StringSocket by doing
    /// 
    ///    ss.BeginSend("Hello world", callback, payload);
    ///    
    /// where callback is a SendCallback (see below) and payload is an arbitrary object.
    /// This is a non-blocking, asynchronous operation.  When the StringSocket has 
    /// successfully written the string to the underlying Socket, or failed in the 
    /// attempt, it invokes the callback.  The parameters to the callback are a
    /// (possibly null) Exception and the payload.  If the Exception is non-null, it is
    /// the Exception that caused the send attempt to fail.
    /// 
    /// We can read a string from the StringSocket by doing
    /// 
    ///     ss.BeginReceive(callback, payload)
    ///     
    /// where callback is a ReceiveCallback (see below) and payload is an arbitrary object.
    /// This is non-blocking, asynchronous operation.  When the StringSocket has read a
    /// string of text terminated by a newline character from the underlying Socket, or
    /// failed in the attempt, it invokes the callback.  The parameters to the callback are
    /// a (possibly null) string, a (possibly null) Exception, and the payload.  Either the
    /// string or the Exception will be non-null, but nor both.  If the string is non-null, 
    /// it is the requested string (with the newline removed).  If the Exception is non-null, 
    /// it is the Exception that caused the send attempt to fail.
    /// </summary>

    public class StringSocket
    {
        /// <summary>
        /// The type of delegate that is called when a send has completed.
        /// </summary>
        public delegate void SendCallback(Exception e, object payload);

        /// <summary>
        /// The type of delegate that is called when a receive has completed.
        /// </summary>
        public delegate void ReceiveCallback(String s, Exception e, object payload);
       
        /// <summary>
        /// Contains information about a single queued send request
        /// </summary>
        private struct SendRequest
        {
            public string Text { get; set; }
            public SendCallback Callback { get; set; }
            public object Payload { get; set; }
        }
     
        /// <summary>
        /// Contains information about a singe queued receive request.
        /// </summary>
        private struct ReceiveRequest
        {
            public ReceiveCallback Callback { get; set; }
            public object Payload { get; set; }
            public int Length { get; set; }
        }

        // Underlying socket
        private Socket socket;

        // Information about strings that are waiting to be sent
        private Queue<SendRequest> sendRequests;

        // Information about strings that are waiting to be received
        private Queue<ReceiveRequest> receiveRequests;

        // Encoding to convert bytes to strings, and associated decoder
        private Encoding encoding;
        private Decoder decoder;

        // Array used to send bytes to the underlying socket
        private byte[] sendBytes;

        // Number of bytes sent to the underlying socket during the current send attempt
        private int sendCount;

        // Array used to receive bytes and chars from the underlying socket
        private byte[] receiveBytes;
        private char[] receiveChars;

        // Incomplete received line
        private String incompleteLine;

        // Complete lines of text received but not yet send to callbacks
        private Queue<string> receivedLines;


        /// <summary>
        /// Creates a StringSocket from a regular Socket, which should already be connected.  
        /// The read and write methods of the regular Socket must not be called after the
        /// StringSocket is created.  Otherwise, the StringSocket will not behave properly.  
        /// The encoding to use to convert between raw bytes and strings is also provided.
        /// </summary>
        public StringSocket(Socket s, Encoding e)
        {
            socket = s;
            encoding = e;
            decoder = encoding.GetDecoder();
            sendRequests = new Queue<SendRequest>();
            receiveRequests = new Queue<ReceiveRequest>();
            incompleteLine = "";
            receiveBytes = new byte[1024];
            receiveChars = new char[1024];
            receivedLines = new Queue<string>();
        }

        /// <summary>
        /// Shuts down and closes the socket.
        /// </summary>
        public void Shutdown  ()
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// We can write a string to a StringSocket ss by doing
        /// 
        ///    ss.BeginSend("Hello world", callback, payload);
        ///    
        /// where callback is a SendCallback (see below) and payload is an arbitrary object.
        /// This is a non-blocking, asynchronous operation.  When the StringSocket has 
        /// successfully written the string to the underlying Socket, or failed in the 
        /// attempt, it invokes the callback.  The parameters to the callback are a
        /// (possibly null) Exception and the payload.  If the Exception is non-null, it is
        /// the Exception that caused the send attempt to fail. 
        /// 
        /// This method is non-blocking.  This means that it does not wait until the string
        /// has been sent before returning.  Instead, it arranges for the string to be sent
        /// and then returns.  When the send is completed (at some time in the future), the
        /// callback is called on another thread.
        /// 
        /// This method is thread safe.  This means that multiple threads can call BeginSend
        /// on a shared socket without worrying around synchronization.  The implementation of
        /// BeginSend must take care of synchronization instead.  On a given StringSocket, each
        /// string arriving via a BeginSend method call must be sent (in its entirety) before
        /// a later arriving string can be sent.
        /// </summary>
        public void BeginSend(String s, SendCallback callback, object payload)
        {
            // Add the request to the queue and launch the sending process if the queue
            // was previously empty.  Otherwise, the sending process is in process and
            // this new entry will be dealt with.
            lock (sendRequests)
            {
                sendRequests.Enqueue(new SendRequest { Text = s, Callback = callback, Payload = payload });
                if (sendRequests.Count == 1)
                {
                    ProcessSendQueue();
                }
            }
        }

        /// <summary>
        /// This should be called only after a lock on sendRequests has been acquired.
        /// It pings back and forth with the BytesSent callback to send out all the strings in
        /// the queue.  This method gets the string at the front of the queue and attempts
        /// to send it.  BytesSent takes care of making sure all of the bytes are actually sent
        /// before calling this method again to send the next string. 
        /// </summary>
        private void ProcessSendQueue()
        {
            while (sendRequests.Count > 0)
            {
                sendBytes = encoding.GetBytes(sendRequests.First().Text);
                try
                {
                    socket.BeginSend(sendBytes, sendCount = 0, sendBytes.Length, SocketFlags.None, BytesSent, null);
                    break;
                }
                catch (Exception e)
                {
                    SendRequest req = sendRequests.Dequeue();
                    ThreadPool.QueueUserWorkItem(x => req.Callback(e, req.Payload));
                }
            }
        }

        /// <summary>
        /// This method is the callback used when bytes are being sent.  It makes sure that all of
        /// the bytes have been sent, then calls the appropriate callback and calls ProcessSendQueue.
        /// </summary>
        private void BytesSent(IAsyncResult ar)
        {
            try
            {
                // Compute how many bytes have been sent so far
                sendCount += socket.EndSend(ar);
            }
            catch (Exception e)
            {
                SendRequest req = sendRequests.Dequeue();
                ThreadPool.QueueUserWorkItem(x => req.Callback(e, req.Payload));
                ProcessSendQueue();
                return;
            }

            // If all the bytes were sent, remove the request from the queue, notify the
            // callback, and process the next entry in the send queue.
            if (sendCount == sendBytes.Length)
            {
                lock (sendRequests)
                {
                    SendRequest req = sendRequests.Dequeue();
                    ThreadPool.QueueUserWorkItem(x => req.Callback(null, req.Payload));
                    ProcessSendQueue();
                }
            }

            // If all the bytes weren't sent, send the rest.
            else
            {
                try
                {
                    socket.BeginSend(sendBytes, sendCount, sendBytes.Length - sendCount, SocketFlags.None, BytesSent, null);
                }

                catch (Exception e)
                {
                    SendRequest req = sendRequests.Dequeue();
                    ThreadPool.QueueUserWorkItem(x => req.Callback(e, req.Payload));
                    ProcessSendQueue();
                }
            }
        }

        /// <summary>
        /// We can read a string from the StringSocket by doing
        /// 
        ///     ss.BeginReceive(callback, payload)
        ///     
        /// where callback is a ReceiveCallback (see below) and payload is an arbitrary object.
        /// This is non-blocking, asynchronous operation.  When the StringSocket has read a
        /// string of text terminated by a newline character from the underlying Socket, or
        /// failed in the attempt, it invokes the callback.  The parameters to the callback are
        /// a (possibly null) string, a (possibly null) Exception, and the payload.  Either the
        /// string or the Exception will be null, or possibly boh.  If the string is non-null, 
        /// it is the requested string (with the newline removed).  If the Exception is non-null, 
        /// it is the Exception that caused the send attempt to fail.  If both are null, this
        /// indicates that the sending end of the remote socket has been shut down.
        /// 
        /// This method is non-blocking.  This means that it does not wait until a line of text
        /// has been received before returning.  Instead, it arranges for a line to be received
        /// and then returns.  When the line is actually received (at some time in the future), the
        /// callback is called on another thread.
        /// 
        /// This method is thread safe.  This means that multiple threads can call BeginReceive
        /// on a shared socket without worrying around synchronization.  The implementation of
        /// BeginReceive must take care of synchronization instead.  On a given StringSocket, each
        /// arriving line of text must be passed to callbacks in the order in which the corresponding
        /// BeginReceive call arrived.
        /// 
        /// Note that it is possible for there to be incoming bytes arriving at the underlying Socket
        /// even when there are no pending callbacks.  StringSocket implementations should refrain
        /// from buffering an unbounded number of incoming bytes beyond what is required to service
        /// the pending callbacks.
        /// </summary>
        public void BeginReceive(ReceiveCallback callback, object payload, int length = 0)
        {
            // Add the request to the queue, then start the receiving process if the queue
            // was previously empty.
            lock (receiveRequests)
            {
                receiveRequests.Enqueue(new ReceiveRequest { Callback = callback, Payload = payload, Length = length });
                if (receiveRequests.Count == 1)
                {
                    ProcessReceiveQueue();
                }
            }
        }

        /// <summary>
        /// This  tries to fill requests with text on hand and then, if there are still
        /// requests remaining, requests more data from the underlying socket.
        /// </summary>
        private void ProcessReceiveQueue()
        {
            lock (receiveRequests)
            {
                // While there is sufficient text, service the callbacks.
                while (receiveRequests.Count() > 0)
                {
                    // Deal with a line request
                    if (receiveRequests.Peek().Length <= 0)
                    {
                        if (receivedLines.Count > 0)
                        {
                            String line = receivedLines.Dequeue();
                            ReceiveRequest req = receiveRequests.Dequeue();
                            ThreadPool.QueueUserWorkItem(x => req.Callback(line, null, req.Payload));
                        }
                        else
                        {
                            break;
                        }
                    }   
                    
                    // Deal with a character request
                    else
                    {
                        string chunk = ExtractChunk(receiveRequests.Peek().Length);
                        if (chunk != null)
                        {
                            ReceiveRequest req = receiveRequests.Dequeue();
                            ThreadPool.QueueUserWorkItem(x => req.Callback(chunk, null, req.Payload));
                        }
                        else
                        {
                            break;
                        }
                    }        
                }

                // If any unserviced requests remain, request more bytes.
                while (receiveRequests.Count > 0)
                {
                    try
                    {
                        socket.BeginReceive(receiveBytes, 0, receiveBytes.Length, SocketFlags.None, BytesReceived, null);
                        break;
                    }
                    catch (Exception e)
                    {
                        ReceiveRequest req = receiveRequests.Dequeue();
                        ThreadPool.QueueUserWorkItem(x => req.Callback(null, e, req.Payload));
                        incompleteLine = "";
                    }
                }
            }
        }

        /// <summary>
        /// Extracts a string of the specified length from the queued lines plus the incomplete line.
        /// </summary>
        private string ExtractChunk (int length)
        {
            int size = 0;
            foreach (string line in receivedLines)
            {
                size += line.Length + 1;
                if (size >= length) break;
            }

            if (size + incompleteLine.Length < length)
            {
                return null;
            }

            string chunk = "";
            while (receivedLines.Count > 0)
            {
                string line = receivedLines.Dequeue();
                if (chunk.Length + line.Length+1 <= length)
                {
                    chunk += line + '\n';
                }
                else
                {
                    int needed = length - chunk.Length;
                    chunk += line.Substring(0, needed);
                    receivedLines.Enqueue(line.Substring(needed));
                }

                if (chunk.Length == length) return chunk;
            }

            int needs = length - chunk.Length;
            chunk += incompleteLine.Substring(0, needs);
            incompleteLine = incompleteLine.Substring(needs);
            return chunk;
        }

        /// <summary>
        /// This private method is the callback for the receive attempts.
        /// </summary>
        private void BytesReceived(IAsyncResult ar)
        {
            // Get the number of bytes received.  
            int count;
            try
            {
                count = socket.EndReceive(ar);
            }
            catch (Exception e)
            {
                ReceiveRequest req = receiveRequests.Dequeue();
                ThreadPool.QueueUserWorkItem(x => req.Callback(null, e, req.Payload));
                ProcessReceiveQueue();
                incompleteLine = "";
                return;
            }

            // If no bytes were received, this means that the remote socket has
            // shut down.  Send a null to the callback to signal this.
            if (count == 0)
            {
                receivedLines.Enqueue(null);
                ProcessReceiveQueue();
            }

            // If bytes were received, save them.
            else
            {
                //incompleteLine += encoding.GetString(receiveBytes, 0, count);
                int charsRead = decoder.GetChars(receiveBytes, 0, count, receiveChars, 0, false);
                incompleteLine += new String(receiveChars, 0, charsRead);

                // Extract all complete lines of text and put into the ReceivedLines queue
                int lineEnd, lineStart = 0;
                while ((lineEnd = incompleteLine.IndexOf('\n', lineStart)) >= 0)
                {
                    receivedLines.Enqueue(incompleteLine.Substring(lineStart, lineEnd - lineStart));
                    lineStart = lineEnd + 1;
                }
                incompleteLine = incompleteLine.Substring(lineStart);

                // Try to fill requests with the new data
                ProcessReceiveQueue();
            }
        }
    }
}