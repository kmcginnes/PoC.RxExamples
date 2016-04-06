using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reflection;

namespace PoC.RxExamples
{
    // Pulled from GitHub decompiled
    public static class Profiler
    {
        private static Type markersType = Type.GetType("Microsoft.ConcurrencyVisualizer.Instrumentation.Markers, Microsoft.ConcurrencyVisualizer.Markers, Version=12.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false);
        private const string MarkersTypeName = "Microsoft.ConcurrencyVisualizer.Instrumentation.Markers, Microsoft.ConcurrencyVisualizer.Markers, Version=12.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
        private static MethodInfo enterSpan;
        private static MethodInfo writeFlag;
        private static MethodInfo writeMessage;

        static Profiler()
        {
            if (markersType == null)
                return;
            enterSpan = markersType.GetMethods(BindingFlags.Static | BindingFlags.Public).First(x =>
            {
                if (x.Name != "EnterSpan")
                    return false;
                ParameterInfo[] parameters = x.GetParameters();
                if (parameters.Length == 3)
                    return parameters[2].ParameterType == typeof(string);
                return false;
            });
            writeFlag = markersType.GetMethods(BindingFlags.Static | BindingFlags.Public).First(x =>
            {
                if (x.Name != "WriteFlag")
                    return false;
                ParameterInfo[] parameters = x.GetParameters();
                if (parameters.Length == 2 && parameters[0].ParameterType != typeof(int))
                    return parameters[1].ParameterType == typeof(string);
                return false;
            });
            writeMessage = markersType.GetMethods(BindingFlags.Static | BindingFlags.Public).First(x =>
            {
                if (x.Name != "WriteMessage")
                    return false;
                ParameterInfo[] parameters = x.GetParameters();
                if (parameters.Length == 1)
                    return parameters[0].ParameterType == typeof(string);
                return false;
            });
        }

        public static IDisposable EnterSpan(string message, Importance importance = Importance.Normal)
        {
            if (markersType == null)
                return Disposable.Empty;
            return (IDisposable)enterSpan.Invoke(null, new object[3]
            {
                importance,
                1,
                message
            });
        }

        public static void Write(string message, Importance importance = Importance.Normal)
        {
            if (markersType == null)
                return;
            switch (importance)
            {
                case Importance.Critical:
                case Importance.High:
                    writeFlag.Invoke(null, new object[2]
                    {
                        importance,
                        message
                    });
                    break;
                default:
                    writeMessage.Invoke(null, new object[1]
                    {
                        message
                    });
                    break;
            }
        }
    }
}