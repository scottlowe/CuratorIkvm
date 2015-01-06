module CuratorIkvm.Tests.Specs

open System
open System.Threading
open NUnit.Framework
open org.apache.curator.framework
open org.apache.curator.framework.recipes.leader
open org.apache.curator.retry

let latchPath = "/test_latch" + Guid.NewGuid().ToString()
let connString = "192.168.59.103:2181"
let sessionTimeout = 5000
let connectionTimeout = 5000

type LeaderClient = {
    Client: CuratorFramework
    Latch: LeaderLatch
}

let instantiateClients() =
    seq {
        for _ in [1 .. 7] ->
            let client = CuratorFrameworkFactory.newClient(connString, new ExponentialBackoffRetry(1000, Int32.MaxValue))
            client.start();
            client.getZookeeperClient().blockUntilConnectedOrTimedOut() |> ignore
            let latch = new LeaderLatch(client, latchPath, Guid.NewGuid().ToString())
            latch.start()
            { Client = client; Latch = latch }
    }


[<TestFixture>]
type CuratorLeaderLatchSanityCheck() =
    let mutable clients = Seq.empty

    [<TestFixtureSetUp>]
    member csc.TestFixtureSetUp() =
        clients <- instantiateClients()

    [<Test>]
    member tc.TestElections() =
        for client in clients do
            while (not (client.Latch.hasLeadership())) do
                Thread.Sleep 20
            client.Latch.close()
        Assert.Pass()

    [<TearDown>]
    member this.DisposeClients () =
        clients
        |> Seq.iter (fun client ->
            client.Latch.close()
            client.Client.close()
            client.Client.Dispose())