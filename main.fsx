#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
open Akka
open Akka.Actor
open Akka.FSharp
open System

let system =
    System.create "system" (Configuration.defaultConfig ())

let random = Random()
let mutable Peers = 4000
let mutable peerReferenceMap = Map.empty
let mutable peerDB = Map.empty

type peerDataRecord = struct
    val mutable peerID: int
    val mutable isAttackerBot: bool
    val mutable status: String
    val mutable timeToLive: int
    val mutable neighborPeers: Set<int>
    val mutable triedPeers: Set<int>
    val mutable testedPeers: Set<int>

    new (pid,ab,stat,ttl,np,tdp,tstp) = {peerID= pid; isAttackerBot= ab; status= stat; timeToLive= ttl; neighborPeers= np; triedPeers= tdp; testedPeers= tstp}
end

type peerInterface = 
    | InitializePeerData of ref<Map<int,peerDataRecord>>*int
    // | FillNewTable of ref<Map<int,peerDataRecord>>*int

let peer (mailbox: Actor<_>) = 
    let rec loop() =  actor {
        let! msg = mailbox.Receive()
        let send = mailbox.Sender()
        match msg with
        | InitializePeerData(peerDB, peerId)->
            let mutable pdb = peerDB.Value
            peerDB.Value <- pdb.Add(peerId, new peerDataRecord(peerId, false, "online", random.Next(120,400), Set.empty, Set.empty, Set.empty))
            printfn "PeerDB ---> %i---> %A" peerId peerDB.Value.[peerId]
            // mailbox.Self <! FillNewTable(peerDB, peerId)
        
        // | FillNewTable(peerDB, peerId)-> 
        //     let newTableCount = random.Next(0, 64)
        //     let mutable pdb = !peerDB
        //     while pdb.[peerId].testedPeers.Count <> newTableCount do 
        //         let newPeer = random.Next(1,500)
        //         if (not (pdb.[peerId].testedPeers.Contains(newPeer))) then do
        //             pdb.[peerId].testedPeers<-pdb.[peerId].testedPeers.Add(newPeer)
        //     !peerDB.[peerId].testedPeers <- pdb.[peerId].testedPeers

        // | AddPeerNeighbors(peerId)->
                

        return! loop ()     
    }
    loop()


let peerProcessStart (totalPeers:int) =
    for peerId = 1 to totalPeers do 
        peerReferenceMap <- peerReferenceMap.Add(peerId, spawn system (sprintf "actor%i" peerId) peer)

    
    for peerId = 1 to totalPeers do
        let mutable pdb = ref peerDB
        peerReferenceMap.[peerId] <! InitializePeerData(pdb, peerId)
    
let var = int fsi.CommandLineArgs.[1]
peerProcessStart(var)