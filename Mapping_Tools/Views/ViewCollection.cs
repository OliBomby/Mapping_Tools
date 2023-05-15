using Mapping_Tools.Classes;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;

namespace Mapping_Tools.Views {
    public class ViewCollection {
        private static readonly Type AcceptableType = typeof(UserControl);
        private static readonly Type MappingToolType = typeof(MappingTool);
        private static readonly Type QuickRunType = typeof(IQuickRun);

        public Dictionary<Type, object> Views = new Dictionary<Type, object>();

        private static Type[] allViewTypes;
        public static Type[] GetAllViewTypes() {
            return allViewTypes ?? (allViewTypes = AppDomain.CurrentDomain.GetAssemblies()
                       .SelectMany(x => x.GetTypes())
                       .Where(x => AcceptableType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract).ToArray());
        }

        private static Type[] allToolTypes;
        public static Type[] GetAllToolTypes() {
            return allToolTypes ?? (allToolTypes = GetAllViewTypes()
                       .Where(x => MappingToolType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract).ToArray());
        }

        private static Type[] allQuickRunTypes;
        public static Type[] GetAllQuickRunTypes() {
            return allQuickRunTypes ?? (allQuickRunTypes = GetAllToolTypes()
                       .Where(x => QuickRunType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract).ToArray());
        }

        public static Type[] GetAllQuickRunTypesWithTargets(SmartQuickRunTargets targets) {
            return GetAllQuickRunTypes().Where(o => {
                    var attribute = o.GetCustomAttribute<SmartQuickRunUsageAttribute>();
                    return attribute != null && attribute.Targets.HasFlag(targets);
                })
                .ToArray();
        }

        public static bool ViewExists(string name) {
            return GetAllViewTypes().Any(o => GetName(o) == name);
        }

        public static string[] GetNames(Type[] types) {
            return types.Where(o => o.GetField("ToolName") != null)
                .Select(GetName).ToArray();
        }

        public static string GetName(Type type) {
            return type.GetField("ToolName") == null ? type.ToString() : type.GetField("ToolName").GetValue(null).ToString();
        }

        public static string GetDescription(Type type) {
            return type.GetField("ToolDescription") == null ? "" : type.GetField("ToolDescription").GetValue(null).ToString();
        }

        public static Type GetType(string name) {
            return GetAllViewTypes().FirstOrDefault(o => GetName(o) == name);
        }

        public object GetView(Type type) {
            try {
                if (!Views.ContainsKey(type)) {
                    object newView = Activator.CreateInstance(type);
                    Views.Add(type, newView);

                    // Attach event handler for QuickRun tools
                    if (newView is IQuickRun qr) {
                        qr.RunFinished += ListenerManager.RunFinishedEventHandler;
                    }
                }
            } catch (Exception ex) {
                ex.Show();
                return null;
            }
            return Views[type];
        }

        public object GetView(string name) {
            var type = GetType(name);
            if (type == null) {
                throw new ArgumentException($"There exists no view with name '{name}'");
            }

            return GetView(type);
        }

        public void AutoSaveSettings() {
            foreach (var kvp in Views.Where(kvp => ProjectManager.IsSavable(kvp.Key))) {
                ProjectManager.AutoSaveProject((dynamic)kvp.Value);
            }
        }
    }
}
