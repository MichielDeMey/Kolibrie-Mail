#region Microsoft Public License (Ms-PL)

// // Microsoft Public License (Ms-PL)
// // 
// // This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.
// // 
// // 1. Definitions
// // 
// // The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under U.S. copyright law.
// // 
// // A "contribution" is the original software, or any additions or changes to the software.
// // 
// // A "contributor" is any person that distributes its contribution under this license.
// // 
// // "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// // 
// // 2. Grant of Rights
// // 
// // (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// // 
// // (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// // 
// // 3. Conditions and Limitations
// // 
// // (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// // 
// // (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
// // 
// // (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
// // 
// // (D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
// // 
// // (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.

#endregion

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace Crystalbyte.Equinox.Security
{
    public abstract class SecureClient : IDisposable
    {
        private readonly TcpClient _tcpClient = new TcpClient();
        private StreamReader _reader;
        private SslStream _secureStream;
        private StreamWriter _writer;

        protected SecureClient()
        {
            Security = SecurityPolicies.Implicit;
            Certificates = new X509Certificate2Collection();
        }

        public X509Certificate2Collection Certificates { get; private set; }

        public SecurityPolicies Security { get; set; }

        public bool IsConnected
        {
            get { return _tcpClient.Connected; }
        }

        public bool IsSecure
        {
            get { return _secureStream != null && _secureStream.IsEncrypted; }
        }

        public bool IsSigned
        {
            get { return _secureStream != null && _secureStream.IsSigned; }
        }

        #region IDisposable Members

        public virtual void Dispose()
        {
            if (IsConnected) {
                Disconnect();
            }
        }

        #endregion

        public event EventHandler<EncryptionProtocolNegotiatedEventArgs> EncryptionProtocolNegotiated;

        private void InvokeEncryptionProtocolNegotiated(SslProtocols protocol, int strength)
        {
            var handler = EncryptionProtocolNegotiated;
            if (handler != null) {
                var e = new EncryptionProtocolNegotiatedEventArgs(protocol, strength);
                handler(this, e);
            }
        }

        public event EventHandler<RemoteCertificateValidationFailedEventArgs> RemoteCertificateValidationFailed;

        private bool InvokeRemoteCertificateValidationFailed(X509Certificate cert, X509Chain chain, SslPolicyErrors error)
        {
            var handler = RemoteCertificateValidationFailed;
            if (handler != null) {
                var e = new RemoteCertificateValidationFailedEventArgs(cert, chain, error);
                handler(this, e);
                return !e.IsCancelled;
            }
            return false;
        }

        protected void WriteLine(string text)
        {
            Debug.WriteLine(string.Format("{0} =>> {1}", DateTime.Now.ToString("T"), text));
            _writer.WriteLine(text);
        }

        protected void Write(string text)
        {
            _writer.Write(text);
        }

        protected void WriteLineAsync(string text)
        {
            ThreadPool.QueueUserWorkItem(x => WriteLine(text));
        }

        protected void WriteAsync(string text)
        {
            ThreadPool.QueueUserWorkItem(x => Write(text));
        }

        protected string ReadLine()
        {
            var line = _reader.ReadLine();
            Debug.WriteLine(string.Format("{0} << {1}", DateTime.Now.ToString("T"), line));
            return line;
        }

        protected int Read()
        {
            return _reader.Read();
        }

        public Stream GetStream()
        {
            if (IsSecure) {
                return _secureStream;
            }
            return _tcpClient.GetStream();
        }

        public virtual void Connect(string host, int port)
        {
            _tcpClient.Connect(host, port);

            var stream = _tcpClient.GetStream();
            _reader = new StreamReader(stream, Encoding.UTF8, false);
            _writer = new StreamWriter(stream) {AutoFlush = true};

            if (Security == SecurityPolicies.Explicit) {
                NegotiateEncryptionProtocols(host);
                ReadWelcomeMessage();
                FetchCapabilities();
            } else {
                ReadWelcomeMessage();
                FetchCapabilities();
                if (Security == SecurityPolicies.Implicit) {
                    var isTlsSupported = IsTlsSupported();
                    if (isTlsSupported) {
                        var success = IssueStartTlsCommand(host);
                        if (success) {
                            NegotiateEncryptionProtocols(host);
                            // It is suggested to update server capabilities after the initial tls negotiation
                            // since some servers may send different capabilities to authenticated clients.
                            FetchCapabilities();
                        }
                    }
                }
            }
        }

        protected virtual void ReadWelcomeMessage()
        {
            ReadLine();
        }

        protected abstract void FetchCapabilities();
        protected abstract bool IsTlsSupported();
        protected abstract bool IssueStartTlsCommand(string host);

        public void Disconnect()
        {
            _writer.Dispose();
            _reader.Dispose();
            _tcpClient.Close();
        }

        protected void NegotiateEncryptionProtocols(string host)
        {
            var stream = _tcpClient.GetStream();
            _secureStream = new SslStream(stream, false, (sender, cert, chain, error) => OnRemoteCertificateValidationCallback(cert, chain, error));
            _secureStream.AuthenticateAsClient(host, Certificates, SslProtocols.Ssl3 | SslProtocols.Tls, true);

            _reader = new StreamReader(_secureStream, Encoding.UTF8, false);
            _writer = new StreamWriter(_secureStream) {AutoFlush = true};

            InvokeEncryptionProtocolNegotiated(_secureStream.SslProtocol, _secureStream.CipherStrength);
        }

        private bool OnRemoteCertificateValidationCallback(X509Certificate cert, X509Chain chain, SslPolicyErrors error)
        {
            return error == SslPolicyErrors.None || InvokeRemoteCertificateValidationFailed(cert, chain, error);
        }
    }
}