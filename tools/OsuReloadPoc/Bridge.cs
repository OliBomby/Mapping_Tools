using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

namespace OsuReloadPoc
{
    /// <summary>Experimental IPC bridge loaded into one inspected osu! stable build.</summary>
    public static class Bridge
    {
        private static string directory;
        private static Module osuModule;
        private static int started;
        private static EditorCircle circle;

        /// <summary>Starts a temporary localhost listener in the existing CLR AppDomain.</summary>
        /// <param name="outputDirectory">Directory for diagnostic logs and the private connection token.</param>
        /// <returns>Zero when initialized; one if initialization failed.</returns>
        public static int Start(string outputDirectory)
        {
            directory = outputDirectory;
            try
            {
                if (Interlocked.Exchange(ref started, 1) != 0) return 0;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    if (assembly.GetName().Name == "osu!") osuModule = assembly.ManifestModule;
                if (osuModule == null || osuModule.ModuleVersionId != new Guid("aa9db4d0-6aa6-4bb7-2f5e-8c1ae12ad43a"))
                    throw new InvalidOperationException("The loaded osu! build does not match the inspected proof-of-concept profile.");
                Log("Attached to PID " + Process.GetCurrentProcess().Id + "; MVID " + osuModule.ModuleVersionId);
                circle = new EditorCircle(osuModule);
                Thread thread = new Thread(Serve) { IsBackground = true, Name = "Mapping Tools IPC proof of concept" };
                thread.Start();
                return 0;
            }
            catch (Exception exception)
            {
                Log(exception.ToString());
                return 1;
            }
        }

        private static void Serve()
        {
            string endpoint = Path.Combine(directory, "endpoint-" + Process.GetCurrentProcess().Id + ".txt");
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listener.Start();
                string token = Guid.NewGuid().ToString("N");
                File.WriteAllLines(endpoint, new[] { ((IPEndPoint)listener.LocalEndpoint).Port.ToString(), token });
                Log("IPC ready on " + listener.LocalEndpoint);
                DateTime expires = DateTime.UtcNow.AddMinutes(5);
                DateTime nextCheck = DateTime.UtcNow;
                while (DateTime.UtcNow < expires)
                {
                    if (!listener.Pending())
                    {
                        if (DateTime.UtcNow >= nextCheck)
                        {
                            OnGameThread(delegate { circle.RemoveIfEditorChanged(); return "OK tick"; });
                            nextCheck = DateTime.UtcNow.AddMilliseconds(250);
                        }
                        Thread.Sleep(50);
                        continue;
                    }
                    using (var client = listener.AcceptTcpClient())
                    {
                        client.ReceiveTimeout = 5000;
                        client.SendTimeout = 5000;
                        using (var stream = client.GetStream())
                        using (var reader = new StreamReader(stream))
                        using (var writer = new StreamWriter(stream) { AutoFlush = true })
                        {
                            string request = reader.ReadLine();
                            if (request == token + " stop") { writer.WriteLine("OK listener stopped"); break; }
                            if (request == null || !request.StartsWith(token + " ", StringComparison.Ordinal))
                            {
                                writer.WriteLine("ERROR invalid request");
                                continue;
                            }
                            string command = request.Substring(token.Length + 1);
                            try
                            {
                                string response = OnGameThread(delegate { return Dispatch(command); });
                                Log(response);
                                writer.WriteLine(response);
                            }
                            catch (Exception exception)
                            {
                                Log(exception.ToString());
                                writer.WriteLine("ERROR " + exception.GetBaseException().Message);
                            }
                        }
                    }
                }
            }
            catch (Exception exception) { Log(exception.ToString()); }
            finally
            {
                listener.Stop();
                try { Log(OnGameThread(delegate { return circle.Clear(); })); }
                catch (Exception exception) { Log("Overlay cleanup failed: " + exception.GetBaseException().Message); }
                if (File.Exists(endpoint)) File.Delete(endpoint);
                Log("IPC listener stopped.");
            }
        }

        private static string Dispatch(string command)
        {
            circle.RemoveIfEditorChanged();
            string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1 && parts[0] == "clear") return circle.Clear();
            if (parts.Length == 1 && parts[0] == "status") return circle.Status();
            if (parts.Length == 1 && parts[0] == "capture")
            {
                // Existing read-only framebuffer helper; no screenshot counter, upload, or game files touched.
                using (var screenshot = (MemoryStream)osuModule.ResolveMethod(0x06001DBC).Invoke(null, null))
                {
                    if (screenshot == null) throw new InvalidOperationException("osu!'s framebuffer capture failed.");
                    screenshot.Position = 0;
                    // The native helper writes RGBA bytes into a BGRA bitmap. Correct its swapped red/blue channels.
                    using (var source = Image.FromStream(screenshot))
                    using (var bitmap = new Bitmap(source.Width, source.Height))
                    using (var graphics = Graphics.FromImage(bitmap))
                    using (var attributes = new ImageAttributes())
                    {
                        attributes.SetColorMatrix(new ColorMatrix(new[]
                        {
                            new[] { 0f, 0f, 1f, 0f, 0f },
                            new[] { 0f, 1f, 0f, 0f, 0f },
                            new[] { 1f, 0f, 0f, 0f, 0f },
                            new[] { 0f, 0f, 0f, 1f, 0f },
                            new[] { 0f, 0f, 0f, 0f, 1f },
                        }));
                        graphics.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height),
                            0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
                        bitmap.Save(Path.Combine(directory, "framebuffer.png"), ImageFormat.Png);
                    }
                    return "OK framebuffer saved bytes=" + screenshot.Length;
                }
            }
            object editor = osuModule.ResolveField(0x040022D5).GetValue(null);
            object currentMode = osuModule.ResolveField(0x04001396).GetValue(null);
            if (editor == null || !ReferenceEquals(editor, currentMode))
                throw new InvalidOperationException("The active osu! screen is not the inspected editor.");
            if (parts.Length == 4 && parts[0] == "circle")
                return circle.Show(editor, Number(parts[1]), Number(parts[2]), Number(parts[3]));
            if (parts.Length == 3 && parts[0] == "move")
                return circle.Move(Number(parts[1]), Number(parts[2]));
            if (parts.Length != 1 || (parts[0] != "probe" && parts[0] != "reload"))
                throw new ArgumentException("Unknown command or invalid argument count.");

            object manager = osuModule.ResolveField(0x040022D8).GetValue(editor);
            object beatmap = osuModule.ResolveField(0x04001E71).GetValue(manager);
            if (beatmap == null) throw new InvalidOperationException("No beatmap is open in the editor.");
            string path = (string)osuModule.ResolveField(0x0400172D).GetValue(beatmap);
            string response = "OK editor=" + editor.GetType().MetadataToken.ToString("X8") +
                " thread=" + Thread.CurrentThread.ManagedThreadId + " beatmap=" + path;
            if (parts[0] == "reload")
            {
                object oldObjects = osuModule.ResolveField(0x040022D7).GetValue(editor);
                // This is the method called by the editor's Ctrl+L branch.
                // false skips its confirmation dialog; false retains the existing audio.
                bool result = (bool)osuModule.ResolveMethod(0x06003655).Invoke(editor, new[] { beatmap, (object)false, false });
                if (!result) throw new InvalidOperationException("osu!'s editor reload method returned false.");
                object newObjects = osuModule.ResolveField(0x040022D7).GetValue(editor);
                response += " reloaded=true objectManagerReplaced=" + !ReferenceEquals(oldObjects, newObjects);
            }
            return response;
        }

        private static float Number(string text)
        {
            float value = float.Parse(text, CultureInfo.InvariantCulture);
            if (float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentException("Coordinates must be finite.");
            return value;
        }

        private static string OnGameThread(Func<string> operation)
        {
            object scheduler = osuModule.ResolveField(0x040013A0).GetValue(null);
            if (scheduler == null) throw new InvalidOperationException("osu!'s game scheduler is unavailable.");
            var enqueue = (MethodInfo)osuModule.ResolveMethod(0x06002F6F);
            var done = new ManualResetEvent(false);
            string response = null;
            Exception failure = null;
            int state = 0;
            Action action = delegate
            {
                if (Interlocked.CompareExchange(ref state, 1, 0) != 0) { done.Dispose(); return; }
                try
                {
                    bool correctThread = (bool)osuModule.ResolveMethod(0x06002F6D).Invoke(scheduler, null);
                    if (!correctThread) throw new InvalidOperationException("Callback is not on osu!'s scheduler thread.");
                    response = operation();
                }
                catch (Exception exception) { failure = exception; }
                finally
                {
                    done.Set();
                    if (Interlocked.Exchange(ref state, 3) == 2) done.Dispose();
                }
            };
            var callback = Delegate.CreateDelegate(enqueue.GetParameters()[0].ParameterType, action.Target, action.Method);
            enqueue.Invoke(scheduler, new object[] { callback, true });
            if (!done.WaitOne(10000))
            {
                int previous = Interlocked.Exchange(ref state, 2);
                if (previous == 3) done.Dispose();
                throw new TimeoutException("The game-thread request timed out.");
            }
            done.Dispose();
            if (failure != null) throw new InvalidOperationException("Game-thread request failed.", failure);
            return response;
        }

        private static void Log(string message)
        {
            File.AppendAllText(Path.Combine(directory, "bridge.log"), DateTime.UtcNow.ToString("O") + " " + message + Environment.NewLine);
        }
    }
}
