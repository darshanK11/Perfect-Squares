#r "nuget: Akka.FSharp"
#time "on"

open System
open Akka.FSharp

type ProcessorJob = Message of double * double * double * double

let mutable exec = true

let mutable count = 0
let mutable totalProcess = 0

let system =
    System.create "system" (Configuration.defaultConfig ())

let calcSqrt (num: bigint) =
    if (num = bigint 0 || num = bigint 1) then
        num
    else
        let mutable startNum = bigint 1
        let mutable endNum = num
        let mutable answer = bigint -1
        let mutable breakFlag = false
        while (startNum <= endNum && not breakFlag) do
            let middleNum = (startNum + endNum) / bigint 2
            if (middleNum * middleNum = num) then
                answer <- middleNum
                breakFlag <- true
            elif (middleNum * middleNum < num) then
                startNum <- middleNum + bigint 1
            else
                endNum <- middleNum - bigint 1
        answer

let calculate (startNum: double) (endNum: double) (k: double) =
    let mutable sum = bigint 0
    let mutable i = startNum
    while (i <= startNum + k - 1.0) do
        sum <- sum + ((bigint i) * (bigint i))
        i <- i + 1.0
    let sqrt = calcSqrt sum
    if (sqrt <> bigint -1) then printfn "%d\n" (int startNum)
    i <- startNum + 1.0
    while (i <= endNum) do
        sum <- sum - (bigint ((i - 1.0) * (i - 1.0)))
        sum <- sum + (bigint ((i + k - 1.0) * (i + k - 1.0)))
        let sqrt = calcSqrt sum
        if (sqrt <> bigint -1) then printfn "%d\n" (int i)
        i <- i + 1.0

let worker (mailbox: Actor<_>) =
    let rec loop () =
        actor {
            let! Message (n, startNum, endNum, k) = mailbox.Receive()
            calculate startNum endNum k
            mailbox.Sender() <! Message(1.0, -1.0, 1.0, 1.0)
        }

    loop ()

let greeter (mailbox: Actor<_>) =
    let rec loop () =
        actor {
            let! Message (n, startNum, endNum, k) = mailbox.Receive()

            if k <= 0.0 || n <= 0.0 then
                exec <- false
            else if startNum = -1.0 then
                totalProcess <- totalProcess + 1
                if totalProcess >= count then exec <- false
            else if (n <= 10.0) then
                let child = spawn system (sprintf "child1") worker
                child <! Message(n, 1.0, n, k)
                count <- 1
                return! loop ()
            else
                let numActor = 10.0
                let range = double (int (n / numActor))
                let mutable i = 1.0
                for j = 1 to (int numActor) do
                    let child =
                        spawn system (sprintf "child%d" j) worker

                    child <! Message(n, i, i + range - 1.0, k)
                    i <- i + range
                count <- (int numActor)
                if (i < n) then
                    let child =
                        spawn system (sprintf "child%d" (count + 1)) worker

                    child <! Message(n, i, n, k)
                    count <- count + 1

            return! loop ()
        }

    loop ()

let arr: string array = fsi.CommandLineArgs |> Array.tail
let n = arr.[0] |> double
let k = arr.[1] |> double

let mutable execCopy = true
let boss = spawn system "Boss" greeter
boss <! Message(n, 0.0, 0.0, k)

while execCopy do
    if not exec then execCopy <- false