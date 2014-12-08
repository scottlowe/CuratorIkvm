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
let summary     = "Curator client library for Zookeeper, IKVM version"
let description = "Curator client library for Zookeeper. This is the IKVM version, which means that it is compiled from Java to .NET"
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

//let genFSAssemblyInfo (projectPath) =
//    let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
//    let basePath = "src/" + projectName
//    let fileName = basePath + "/AssemblyInfo.fs"
//    CreateFSharpAssemblyInfo fileName
//      [ Attribute.Title (projectName)
//        Attribute.Product project
//        Attribute.Description summary
//        Attribute.Version release.AssemblyVersion
//        Attribute.FileVersion release.AssemblyVersion ]
//
//let genCSAssemblyInfo (projectPath) =
//    let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
//    let basePath = "src/" + projectName + "/Properties"
//    let fileName = basePath + "/AssemblyInfo.cs"
//    CreateCSharpAssemblyInfo fileName
//      [ Attribute.Title (projectName)
//        Attribute.Product project
//        Attribute.Description summary
//        Attribute.Version release.AssemblyVersion
//        Attribute.FileVersion release.AssemblyVersion ]

//Target "AssemblyInfo" (fun _ ->
//  let fsProjs =  !! "src/**/*.fsproj"
//  let csProjs = !! "src/**/*.csproj"
//  fsProjs |> Seq.iter genFSAssemblyInfo
//  csProjs |> Seq.iter genCSAssemblyInfo
//)

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

    let result =
        ExecProcess (fun info ->
            info.FileName <- sprintf "lib/ikvm-%s/bin/ikvmc.exe " ikvmVersion
            info.WorkingDirectory <- "./"
            info.Arguments <- "-lib:target/dependency -recurse:target/dependency -target:library -out:bin/Curator.dll")
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
            Dependencies = [] })
        ("nuget/" + project + ".nuspec")
)

//Target "ReleaseDocs" (fun _ ->
//    let tempDocsDir = "temp/gh-pages"
//    CleanDir tempDocsDir
//    Repository.cloneSingleBranch "" (gitHome + "/" + gitName + ".git") "gh-pages" tempDocsDir
//
//    fullclean tempDocsDir
//    CopyRecursive "docs/output" tempDocsDir true |> tracefn "%A"
//    StageAll tempDocsDir
//    Git.Commit.Commit tempDocsDir (sprintf "Update generated documentation for version %s" release.NugetVersion)
//    Branches.push tempDocsDir
//)

#load "paket-files/fsharp/FAKE/modules/Octokit/Octokit.fsx"
open Octokit

Target "Release" (fun _ ->
    StageAll ""
    Git.Commit.Commit "" (sprintf "Bump version to %s" release.NugetVersion)
    Branches.push ""

    Branches.tag "" release.NugetVersion
    Branches.pushTag "" "origin" release.NugetVersion

    // release on github
    createClient (getBuildParamOrDefault "github-user" "") (getBuildParamOrDefault "github-pw" "")
    |> createDraft gitOwner gitName release.NugetVersion (release.SemVer.PreRelease <> None) release.Notes
    // TODO: |> uploadFile "PATH_TO_FILE"
    |> releaseDraft
    |> Async.RunSynchronously
)

Target "BuildPackage" DoNothing

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "All" DoNothing

Target "BuildDll" DoNothing

"BuildDll"
    //==> "DownloadMaven"
    //==> "InstallMavenDeps"
    //==> "DownloadIkvm"
    //==> "Build"
    ==> "All"

//"Clean"
//  ==> "BuildDll"
//  ==> "All"

"All"
  ==> "NuGet"
  //==> "BuildPackage"

"BuildPackage"
  ==> "Release"

RunTargetOrDefault "All"