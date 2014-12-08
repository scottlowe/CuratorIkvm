// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r @"packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open Fake.FileUtils
open Fake.EnvironmentHelper
open System
open System.IO
open System.Net


let project     = "CuratorIkvm"
let summary     = "Curator client library & recipes for Zookeeper: IKVM version."
let description = "Curator client library & recipes for Zookeeper. This is the IKVM version; which is to say that this is compiled from Java to .NET"
let authors     = [ "Scott Lowe" ]
let tags        = "Curator C# .Net IKVM Zookeeper"


let mavenVersion = "3.2.3"
let mavenDirName = sprintf "apache-maven-%s" mavenVersion
let mavenBinariesUrl =
    sprintf "http://mirror.rackcentral.com.au/apache/maven/maven-3/%s/binaries/%s-bin.zip" mavenVersion mavenDirName

let ikvmVersion = "8.0.5449.0"
let ikvmBinariesUrl = sprintf "http://www.frijters.net/ikvmbin-%s.zip" ikvmVersion

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted
let gitOwner = "scottlowe"
let gitHome  = "https://github.com/" + gitOwner
let gitName  = "CuratorIkvm"
let gitRaw   = environVarOrDefault "gitRaw" "https://raw.github.com/scottlowe"

// Read additional information from the release notes document
let release = LoadReleaseNotes "RELEASE_NOTES.md"

let downloadAndUnzipLib (binariesUri: string) (targetZip: string) =
    let wc = new WebClient()
    wc.DownloadFile(new Uri(binariesUri), targetZip)
    Unzip "lib/" targetZip
    rm targetZip

Target "DownloadMaven" (fun _ ->
    downloadAndUnzipLib mavenBinariesUrl @"lib/maven.zip"
)

Target "DownloadIkvm" (fun _ ->
    downloadAndUnzipLib ikvmBinariesUrl @"lib/ikvm.zip"
)

Target "InstallMavenDeps" (fun _ ->
    let result =
        ExecProcess (fun info ->
            info.FileName <- sprintf "lib/%s/bin/mvn.bat" mavenDirName
            info.WorkingDirectory <- "./"
            info.Arguments <- "install dependency:copy-dependencies")
            (TimeSpan.FromMinutes 7.0)

    if result <> 0 then
        failwithf "Maven dependencies install returned a non-zero exit code"
)

Target "Clean" (fun _ ->
    CleanDirs ["bin"; "temp"]
)

Target "CleanDocs" (fun _ ->
    CleanDirs ["docs/output"]
)

Target "Build" (fun _ ->
    traceImportant "building with IKVM..."

    let ikvmArgs =
        let version = release.NugetVersion.Split('-') |> Seq.head
        sprintf
            "-lib:target/dependency -recurse:target/dependency -target:library -version:%s -out:bin/Curator.dll"
            version

    let result =
        ExecProcess (fun info ->
            info.FileName <- sprintf "lib/ikvm-%s/bin/ikvmc.exe " ikvmVersion
            info.WorkingDirectory <- "./"
            info.Arguments <- ikvmArgs)
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
            Version = release.NugetVersion
            ReleaseNotes = String.Join(Environment.NewLine, release.Notes)
            Tags = tags
            OutputPath = "bin"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = [ "IKVM", ikvmVersion ]})
        ("nuget/" + project + ".nuspec")
)

#load "paket-files/fsharp/FAKE/modules/Octokit/Octokit.fsx"
open Octokit

Target "Release" (fun _ ->
    StageAll ""
    Git.Commit.Commit "" (sprintf "Bump version to %s" release.NugetVersion)
    Branches.push ""

    Branches.tag "" release.NugetVersion
    Branches.pushTag "" "origin" release.NugetVersion
)

Target "BuildPackage" DoNothing

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "All" DoNothing

Target "BuildDll" DoNothing

"BuildDll"
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