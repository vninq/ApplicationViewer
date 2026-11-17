// See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");
using ClickableTransparentOverlay;
using ImGuiNET;
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ApplicationViewer
{
    public class Program : Overlay
    {
        // hide the console window on startup
        internal static class HideWindow 
        {
            [DllImport("kernel32.dll")]
            public static extern IntPtr GetConsoleWindow();
            [DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        }

        bool loaded = false;

        public const string version = "1.0";

        record Apps(int Id, string Name, string Title, double Memory);

        // program stuff
        private static List<Apps> apps = new List<Apps>();
        private static string search = "";
        private static bool hasWindow = true;
        private DateTime lastTime = DateTime.MinValue;

        // renderer
        protected override void Render()
        {

            if ((DateTime.Now - lastTime).TotalSeconds > 1)
            {
                RefreshProcesses();
                lastTime = DateTime.Now;
            }

            ImGui.Begin($"Process Viewer Version: {version}");

            if (ImGui.Button("Refresh"))
            {
                RefreshProcesses();
            }

            // idk
            /*if (ImGui.Button("END ALL ADOBE PROGRAMS NOW"))
            {
            }*/

            ImGui.SameLine();

            if (ImGui.Checkbox("Hide Programs Without Windows", ref hasWindow))
            {
                RefreshProcesses();
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            ImGui.InputTextWithHint("##search", "Search for a process", ref search, 100);

            ImGui.Separator();

            var filter = string.IsNullOrWhiteSpace(search)
                ? apps
                : apps.Where(a => a.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                a.Title.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

            ImGui.Text($"Showing {filter.Count}");

            // render the table
            if (ImGui.BeginTable("appTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
            {
                ImGui.TableSetupColumn("Id");
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("Title");
                ImGui.TableSetupColumn("Memory");
                ImGui.TableSetupColumn("End task");

                ImGui.TableHeadersRow();

                foreach (var app in filter)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0); ImGui.Text(app.Id.ToString());
                    ImGui.TableSetColumnIndex(1); ImGui.Text(app.Name);
                    ImGui.TableSetColumnIndex(2); ImGui.Text(app.Title);
                    ImGui.TableSetColumnIndex(3); ImGui.Text($"{app.Memory:F1} mb");
                    ImGui.TableSetColumnIndex(4);

                    if (ImGui.SmallButton($"End process##{app.Id}"))
                    {
                        try
                        {
                            Process.GetProcessById(app.Id).Kill();
                            RefreshProcesses();
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Could not end process " + app.Name);
                            Console.ResetColor();
                        }
                    }
                }

                ImGui.EndTable();
            }

            ImGui.End();
        }

        static void Main(string[] args)
        {
            HideWindow.ShowWindow(HideWindow.GetConsoleWindow(), 1);

            Console.WriteLine("Starting");
            Program program = new Program();
            program.Start().Wait();
            Console.WriteLine("Loading processes");
        }

        // process referesher
        private static void RefreshProcesses()
        {
            var processes = Process.GetProcesses();
            var appList = new List<Apps>();

            foreach (var process in processes)
            {
                try
                {
                    if (hasWindow && string.IsNullOrWhiteSpace(process.MainWindowTitle))
                        continue;

                    appList.Add(new Apps(process. Id,process.ProcessName, process.MainWindowTitle, Math.Round(process.WorkingSet64 / 1024.0 / 1024.0, 1)
                    ));
                }
                finally
                {
                    process.Dispose();
                }
            }

            apps = appList.OrderBy(a => a.Name).ToList();
        }
    }
}