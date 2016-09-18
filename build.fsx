// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r @"packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open Fake.FileUtils
open Fake.FileSystemHelper
open Fake.EnvironmentHelper
open System
open System.IO
open System.Net


let project = "CuratorIkvm"
let summary = "Curator client library & recipes for Zookeeper: IKVM version."
let authors = ["Scott Lowe"]
let tags    = "Curator IKVM Zookeeper"

let description =
    "Curator client library & recipes for Zookeeper.
    This is the IKVM version; which is to say that this is compiled from Java to .NET"

let mavenVersion = "3.2.5"
let mavenDirName = sprintf "apache-maven-%s" mavenVersion
let mavenBinariesUrl =
    sprintf "http://mirror.catn.com/pub/apache/maven/maven-3/%s/binaries/%s-bin.zip" mavenVersion mavenDirName

let ikvmVersion = "8.0.5449.1"
let ikvmBinariesUrl = sprintf "http://www.frijters.net/ikvmbin-%s.zip" ikvmVersion

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted
let gitOwner = "scottlowe"
let gitHome  = "https://github.com/" + gitOwner
let gitName  = "CuratorIkvm"
let gitRaw   = environVarOrDefault "gitRaw" "https://raw.github.com/scottlowe"

let releaseNotes = LoadReleaseNotes "RELEASE_NOTES.md"
let pathSeparator = Path.DirectorySeparatorChar.ToString() 
let libDir = sprintf "%s%slib%s" currentDirectory pathSeparator pathSeparator

let downloadAndUnzipLib (binariesUri: string) (targetZip: string) =
    let wc = new WebClient()
    wc.DownloadFile(new Uri(binariesUri), targetZip)
    Unzip libDir targetZip
    rm targetZip

Target "MakeDirectories" (fun _ ->
    traceImportant (sprintf "Making directory %s, if it doesn't exist" libDir)
    mkdir libDir
)

Target "DownloadMaven" (fun _ ->
    downloadAndUnzipLib mavenBinariesUrl (sprintf "%smaven.zip" libDir)
)

Target "DownloadIkvm" (fun _ ->
    downloadAndUnzipLib ikvmBinariesUrl (sprintf "%sikvm.zip" libDir)
)

Target "InstallMavenDeps" (fun _ ->
    let mvnExeName = if isUnix then "mvn" else "mvn.bat"
    //let mvnExe = sprintf "%s%s/bin/%s" libDir mavenDirName mvnExeName
    let mvnExe = Path.Combine(libDir, mavenDirName, "bin", mvnExeName)

    if isUnix then Shell.Exec("chmod", "a+rx " + mvnExe) |> ignore

    let result =
        ExecProcess (fun info ->
            info.FileName <- mvnExe
            info.WorkingDirectory <- "./"
            info.Arguments <- "install dependency:copy-dependencies")
            (TimeSpan.FromMinutes 7.0)

    if result <> 0 then
        failwithf "Maven dependencies install returned a non-zero exit code"
)

Target "Clean" (fun _ ->
    CleanDirs ["bin"; "temp"; "target/dependency"]
)

Target "CleanDocs" (fun _ ->
    CleanDirs ["docs/output"]
)

Target "Build" (fun _ ->
    traceImportant "building with IKVM..."
    let versionedDir = sprintf "ikvm-%s" ikvmVersion
    let ikvmFileExe = Path.Combine(currentDirectory, "lib", versionedDir, "bin", "ikvmc.exe")

    if isUnix then Shell.Exec("chmod", "a+rx " + ikvmFileExe) |> ignore

    let fileName =
        if isUnix then "/usr/bin/mono"
        else ikvmFileExe

    let ikvmArgs =
        let version = releaseNotes.NugetVersion.Split('-') |> Seq.head
        sprintf
            "-lib:target/dependency -recurse:target/dependency -target:library -version:%s -out:bin/Curator.dll"
            version

    let processArgs =
        if isUnix then sprintf "%s %s" ikvmFileExe ikvmArgs
        else ikvmArgs

    let result =
        ExecProcess (fun info ->
            info.FileName <- fileName
            info.WorkingDirectory <- currentDirectory
            info.Arguments <- processArgs)
            (TimeSpan.FromMinutes 5.0)

    if result <> 0 then
        failwithf "IKVM compilation of Curator.dll returned a non-zero exit code"
    ()
)

Target "NuGet" (fun _ ->
    NuGet (fun p ->
        { p with
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = releaseNotes.NugetVersion
            ReleaseNotes = String.Join(Environment.NewLine, releaseNotes.Notes)
            Tags = tags
            OutputPath = "bin"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = [ "IKVM", ikvmVersion ]
            Files = [ (@"..\bin\Curator.dll", Some "lib/net45", None) ]})
        ("nuget/" + project + ".nuspec")
)

#load "paket-files/fsharp/FAKE/modules/Octokit/Octokit.fsx"
open Octokit

Target "Release" (fun _ ->
    StageAll ""
    Git.Commit.Commit "" (sprintf "Bump version to %s" releaseNotes.NugetVersion)
    Branches.push ""

    Branches.tag "" releaseNotes.NugetVersion
    Branches.pushTag "" "origin" releaseNotes.NugetVersion
)

Target "BuildPackage" DoNothing

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "All" DoNothing

Target "BuildDll" DoNothing

"BuildDll"
    ==> "MakeDirectories"
    ==> "DownloadMaven"
    ==> "InstallMavenDeps"
    ==> "DownloadIkvm"
    ==> "Build"
    ==> "All"

"Clean"
  ==> "BuildDll"
  ==> "All"

"All"
  ==> "NuGet"
  ==> "BuildPackage"

"BuildPackage"
  ==> "Release"

RunTargetOrDefault "All"