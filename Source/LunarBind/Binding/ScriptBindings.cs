﻿namespace LunarBind
{
    using MoonSharp.Interpreter;
    using MoonSharp.Interpreter.Interop;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    public class ScriptBindings
    {
        private readonly Dictionary<string, BindItem> callbackItems = new Dictionary<string, BindItem>();
        private readonly Dictionary<string, Type> yieldableTypes = new Dictionary<string, Type>();
        private readonly Dictionary<string, Type> newableTypes = new Dictionary<string, Type>();
        private readonly Dictionary<string, Type> staticTypes = new Dictionary<string, Type>();
        private string bakedTypeString = null;
        private string bakedYieldableTypeString = null;

        /// <summary>
        /// Use this to initialize any scripts initialized with this binder with custom Lua code. This runs after all bindings have been set up
        /// </summary>
        public string CustomInitializerString { get; set; } = null;

        public ScriptBindings()
        {

        }

        public ScriptBindings(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                RegisterAssemblyFuncs(assembly);
                UserData.RegisterAssembly(assembly);
            }
        }

        public ScriptBindings(params Type[] types)
        {
            foreach (var type in types)
            {
                RegisterTypeFuncs(type);
            }
        }

        public ScriptBindings(params object[] objs)
        {
            foreach (var obj in objs)
            {
                RegisterObjectFuncs(obj);
            }
        }

        public ScriptBindings(params Action[] actions)
        {
            AddActions(actions);
        }

        public ScriptBindings(params Delegate[] dels)
        {
            AddDelegates(dels);
        }

        /// <summary>
        /// Allows you to access static functions and members on the type by using the Lua global with the name<para/>
        /// Equivalent to script.Globals[t.Name] = t
        /// </summary>
        /// <param name="t"></param>
        public void AddGlobalType(Type t)
        {
            RegisterUserDataType(t);
            staticTypes[t.Name] = t;
        }

        /// <summary>
        /// Allows you to access static functions and members on the type by using the Lua global with the name<para/>
        /// <para/>
        /// Equivalent to script.Globals[name] = t
        /// </summary>
        /// <param name="name"></param>
        /// <param name="t"></param>
        public void AddGlobalType(string name, Type t)
        {
            RegisterUserDataType(t);
            staticTypes[name] = t;
        }


        public void AddTypes(params Type[] types)
        {
            foreach (var type in types)
            {
                RegisterTypeFuncs(type);
            }
        }
        public void AddObjects(params object[] objs)
        {
            foreach (var obj in objs)
            {
                RegisterObjectFuncs(obj);
            }
        }
        public void AddObjects<T>(params T[] objs)
        {
            foreach (var obj in objs)
            {
                RegisterObjectFuncs(obj);
            }
        }

        public void AddType(Type type)
        {
            RegisterTypeFuncs(type);
        }
        public void AddObject(object obj)
        {
            RegisterObjectFuncs(obj);
        }


        /// <summary>
        /// Add a specific <see cref="Action"/> to the bindings
        /// </summary>
        /// <param name="name"></param>
        /// <param name="action"></param>
        /// <param name="documentation"></param>
        /// <param name="example"></param>
        public void AddAction(string name, Action action, string documentation = "", string example = "")
        {
            BindingHelpers.CreateBindFunction(callbackItems, name, action, documentation ?? "", example ?? "");
            //callbackItems[name] = new CallbackFunc(name, action, documentation, example);
        }
        /// <summary>
        /// Add a specific <see cref="Action"/> to the bindings, using the method's Name as the name
        /// </summary>
        /// <param name="action"></param>
        /// <param name="documentation"></param>
        /// <param name="example"></param>
        public void AddAction(Action action, string documentation = "", string example = "")
        {
            BindingHelpers.CreateBindFunction(callbackItems, action.Method.Name, action, documentation ?? "", example ?? "");
            //callbackItems[action.Method.Name] = new CallbackFunc(action.Method.Name, action, documentation, example);
        }

        /// <summary>
        /// Add specific <see cref="Action"/>s to the bindings, using the method's Name as the name for each
        /// </summary>
        /// <param name="actions"></param>
        public void AddActions(params Action[] actions)
        {
            foreach (var action in actions)
            {
                BindingHelpers.CreateBindFunction(callbackItems, action.Method.Name, action, "", "");
                //callbackItems[action.Method.Name] = new CallbackFunc(action.Method.Name, action);
            }
        }

        /// <summary>
        /// Add a specific <see cref="Delegate"/> to the bindings
        /// </summary>
        /// <param name="name"></param>
        /// <param name="del"></param>
        /// <param name="documentation"></param>
        /// <param name="example"></param>
        public void AddDelegate(string name, Delegate del, string documentation = "", string example = "")
        {
            BindingHelpers.CreateBindFunction(callbackItems, name, del, documentation ?? "", example ?? "");
            //callbackItems[name] = new CallbackFunc(name, del, documentation, example);
        }

        /// <summary>
        /// Add a specific <see cref="Delegate"/> to the bindings using its Name as the name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="del"></param>
        /// <param name="documentation"></param>
        /// <param name="example"></param>
        public void AddDelegate(Delegate del, string documentation = "", string example = "")
        {
            BindingHelpers.CreateBindFunction(callbackItems, del.Method.Name, del, documentation ?? "", example ?? "");
            //callbackItems[del.Method.Name] = new CallbackFunc(del.Method.Name, del, documentation, example);
        }

        /// <summary>
        /// Add specific <see cref="Delegate"/>s to the bindings using the method Name as the name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="del"></param>
        /// <param name="documentation"></param>
        /// <param name="example"></param>
        public void AddDelegates(params Delegate[] dels)
        {
            foreach (var del in dels)
            {
                BindingHelpers.CreateBindFunction(callbackItems, del.Method.Name, del, "", "");
                //callbackItems[del.Method.Name] = new CallbackFunc(del.Method.Name, del);
            }
        }


        ///// <summary>
        ///// Unstable, untested :)
        ///// </summary>
        ///// <typeparam name="T0"></typeparam>
        ///// <param name="target"></param>
        //public void HookActionProps<T0>(object target)
        //{
        //    Type type = target.GetType();
        //    PropertyInfo[] props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //    foreach (var prop in props)
        //    {
        //        var attr = (LuaFunctionAttribute)Attribute.GetCustomAttribute(prop, typeof(LuaFunctionAttribute));
        //        if (attr != null)
        //        {
        //            var val = prop.GetValue(target);
        //            if (val.GetType().IsAssignableFrom(typeof(Action<T0>)))
        //            {
        //                var action = ((Action<T0>)val);
        //                string name = attr.Name ?? prop.Name;
        //                BindingHelpers.CreateCallbackItem(callbackItems, name, action, "", "");
        //                //callbackItems[name] = new CallbackFunc(name, action, "", "");
        //            }
        //        }
        //    }
        //}

        ///// <summary>
        ///// Unstable, untested :)
        ///// </summary>
        ///// <typeparam name="T0"></typeparam>
        ///// <param name="type"></param>
        //public void HookActionProps<T0>(Type type)
        //{
        //    PropertyInfo[] props = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        //    foreach (var prop in props)
        //    {
        //        var attr = (LuaFunctionAttribute)Attribute.GetCustomAttribute(prop, typeof(LuaFunctionAttribute));
        //        if (attr != null)
        //        {
        //            var val = prop.GetValue(null);
        //            if (val.GetType().IsAssignableFrom(typeof(Action<T0>)))
        //            {
        //                var action = ((Action<T0>)val);
        //                string name = attr.Name ?? prop.Name;
        //                callbackItems[name] = new CallbackFunc(name, action, "", "");
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// Automatically register all the static functions with <see cref="Attributes.LuaFunctionAttribute"/> for specific assemblies
        /// </summary>
        /// <param name="assemblies"></param>
        public void AddAssemblies(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                RegisterAssemblyFuncs(assembly);
                UserData.RegisterAssembly(assembly);
            }
        }

        private void RegisterObjectFuncs(object target)
        {
            if (target == null) { throw new ArgumentNullException(nameof(target)); }
            Type type = target.GetType();
            MethodInfo[] mis = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var mi in mis)
            {
                var attr = (LunarBindFunctionAttribute)Attribute.GetCustomAttribute(mi, typeof(LunarBindFunctionAttribute));
                if (attr != null)
                {
                    var documentation = (LunarBindDocumentationAttribute)Attribute.GetCustomAttribute(mi, typeof(LunarBindDocumentationAttribute));
                    var example = (LunarBindExampleAttribute)Attribute.GetCustomAttribute(mi, typeof(LunarBindExampleAttribute));
                    var del = BindingHelpers.CreateDelegate(mi, target);
                    string name = attr.Name ?? mi.Name;
                    BindingHelpers.CreateBindFunction(callbackItems, name, del, documentation?.Data ?? "", example?.Data ?? "");
                    //callbackItems[name] = new CallbackFunc(name, del, documentation?.Data ?? "", example?.Data ?? "");
                }
            }
        }

        private void RegisterTypeFuncs(Type type)
        {
            MethodInfo[] mis = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var mi in mis)
            {
                var attr = (LunarBindFunctionAttribute)Attribute.GetCustomAttribute(mi, typeof(LunarBindFunctionAttribute));
                if (attr != null)
                {
                    var documentation = (LunarBindDocumentationAttribute)Attribute.GetCustomAttribute(mi, typeof(LunarBindDocumentationAttribute));
                    var example = (LunarBindExampleAttribute)Attribute.GetCustomAttribute(mi, typeof(LunarBindExampleAttribute));
                    var del = BindingHelpers.CreateDelegate(mi);
                    string name = attr.Name ?? mi.Name;
                    BindingHelpers.CreateBindFunction(callbackItems, name, del, documentation?.Data ?? "", example?.Data ?? "");
                    //callbackItems[name] = new CallbackFunc(name, del, documentation?.Data ?? "", example?.Data ?? "");
                }
            }
        }

        private void RegisterAssemblyFuncs(Assembly assembly)
        {
            Type[] types = assembly.GetTypes();
            foreach (var type in types)
            {
                RegisterTypeFuncs(type);
            }
        }
       
        public static void RegisterUserDataType(Type t)
        {
            UserData.RegisterType(t);
        }

        public void AddYieldableType<T>(string name = null) where T : Yielder
        {
            if (name == null) { name = typeof(T).Name; }
            RegisterUserDataType(typeof(T));
            yieldableTypes[GlobalScriptBindings.TypePrefix + name] = typeof(T);
            BakeYieldables();
        }

        /// <summary>
        /// Also allows you to access static functions on the type by using _TypeName
        /// </summary>
        /// <param name="name"></param>
        /// <param name="t"></param>
        public void AddNewableType(string name, Type t)
        {
            RegisterUserDataType(t);
            newableTypes[GlobalScriptBindings.TypePrefix + name] = t;
            BakeNewables();
        }

        public void AddNewableType(Type t)
        {
            RegisterUserDataType(t);
            newableTypes[GlobalScriptBindings.TypePrefix + t.Name] = t;
            BakeNewables();
        }

        public void RemoveNewableType(string name)
        {
            newableTypes.Remove(GlobalScriptBindings.TypePrefix + name);
            BakeNewables();
        }

        /// <summary>
        /// Allows you to access static functions on the type by using _TypeName
        /// </summary>
        /// <param name="t"></param>
        public void RegisterStaticType(Type t)
        {
            RegisterUserDataType(t);
            staticTypes[GlobalScriptBindings.TypePrefix + t.Name] = t;
        }


        /// <summary>
        /// Exposed initialize function to initialize non-scriptcore moonsharp scripts
        /// </summary>
        /// <param name="lua"></param>
        public void Initialize(Script lua)
        {
            foreach (var item in callbackItems.Values)
            {
                item.AddToScript(lua);
            }
            foreach (var type in staticTypes)
            {
                lua.Globals[type.Key] = type.Value;
            }

            InitializeNewables(lua);
            InitializeYieldables(lua);

            if (CustomInitializerString != null)
            {
                lua.DoString(CustomInitializerString);
            }
        }

        private static string Bake(Dictionary<string, Type> source)
        {
            StringBuilder s = new StringBuilder();

            foreach (var type in source)
            {
                string typeName = type.Key;
                string newFuncName = type.Key.Remove(0, GlobalScriptBindings.TypePrefix.Length);
                HashSet<int> paramCounts = new HashSet<int>();
                var ctors = type.Value.GetConstructors();
                foreach (var ctor in ctors)
                {
                    var pars = ctor.GetParameters();
                    foreach (var item in pars)
                    {
                        if (!item.ParameterType.IsPrimitive && item.ParameterType != typeof(string) && !UserData.IsTypeRegistered(item.ParameterType))
                        {
                            throw new Exception("CLR type constructor parameters must be added to UserData or be a primitive type or string");
                        }
                    }

                    if (!paramCounts.Contains(pars.Length))
                    {
                        string parString = "";
                        paramCounts.Add(pars.Length);
                        for (int j = 0; j < pars.Length; j++)
                        {
                            if (j == 0) { parString += $"t{j}"; }
                            else { parString += $",t{j}"; }
                        }
                        s.AppendLine($"function {newFuncName}({parString}) return {typeName}.__new({parString}) end");
                    }
                }
            }
            return s.ToString();
        }

        private void BakeNewables()
        {
            bakedTypeString = Bake(newableTypes);
        }

        private void BakeYieldables()
        {
            bakedYieldableTypeString = Bake(yieldableTypes);
        }

        /// <summary>
        /// Initializes a script with the newable types
        /// </summary>
        /// <param name="lua"></param>
        private void InitializeNewables(Script lua)
        {
            foreach (var type in newableTypes)
            {
                lua.Globals[type.Key] = type.Value;
            }

            if (bakedTypeString != null)
            {
                lua.DoString(bakedTypeString);
            }
        }

        private void InitializeYieldables(Script lua)
        {
            foreach (var type in yieldableTypes)
            {
                lua.Globals[type.Key] = type.Value;
            }

            if (bakedYieldableTypeString != null)
            {
                lua.DoString(bakedYieldableTypeString);
            }
        }


        /// <summary>
        /// Removes all callback functions from a script
        /// </summary>
        /// <param name="lua"></param>
        internal void Clean(Script lua)
        {
            foreach (var func in callbackItems)
            {
                lua.Globals.Remove(func.Value.Name);
            }
        }

    }
}
