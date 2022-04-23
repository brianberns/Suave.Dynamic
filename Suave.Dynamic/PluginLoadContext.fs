namespace Suave.Dynamic

open System
open System.IO
open System.Reflection
open System.Runtime.Loader

// https://docs.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support
type PluginLoadContext(pluginPath) =
    inherit AssemblyLoadContext()

    let resolver = AssemblyDependencyResolver(pluginPath)

    override this.Load(assemblyName) =
        let assemblyPath = resolver.ResolveAssemblyToPath(assemblyName)
        if assemblyPath <> null then
            this.LoadFromAssemblyPath(assemblyPath)
        else null

    override this.LoadUnmanagedDll(unmanagedDllName) =
        let libraryPath = resolver.ResolveUnmanagedDllToPath(unmanagedDllName)
        if libraryPath <> null then
            this.LoadUnmanagedDllFromPath(libraryPath)
        else IntPtr.Zero

module PluginLoadContext =

    /// Loads the assembly at the given location.
    let load assemblyPath =

            // why is the assembly path needed twice in this process?
        let loadContext = PluginLoadContext(assemblyPath)
        assemblyPath
            |> Path.GetFileNameWithoutExtension
            |> AssemblyName
            |> loadContext.LoadFromAssemblyName
