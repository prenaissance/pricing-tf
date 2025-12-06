namespace PricingTf.Processing.Actors

open PricingTf.Common.Models

type BlockedUsersAgentMessage =
    | Add of BlockedUser
    | Remove of BlockedUser
    | GetAll of AsyncReplyChannel<Set<string>>
    | Clear

type BlockedUsersAgent(initialSteamIds) =
    let agent =
        MailboxProcessor.Start
        <| fun inbox ->
            let rec loop steamIdSet =
                async {
                    let! msg = inbox.Receive()

                    match msg with
                    | Add user ->
                        let updatedSet = steamIdSet |> Set.add user.steamId
                        return! loop updatedSet
                    | Remove user ->
                        let updatedSet = steamIdSet |> Set.remove user.steamId
                        return! loop updatedSet
                    | GetAll replyChannel ->
                        replyChannel.Reply steamIdSet
                        return! loop steamIdSet
                    | Clear -> return! loop Set.empty
                }

            loop (Set.ofSeq initialSteamIds)

    member _.Add(user: BlockedUser) = agent.Post(Add user)
    member _.Remove(user: BlockedUser) = agent.Post(Remove user)

    member _.GetAll() =
        agent.PostAndAsyncReply <| fun reply -> GetAll reply

    member _.Clear() = agent.Post Clear
