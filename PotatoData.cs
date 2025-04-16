// using System;
// using RainMeadow;

// namespace Meadow_MiniGame_HotPotato
// {
//     public class PotatoData : OnlineEntity.EntityData
//     {
//         public bool isBomb;
//         public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
//         {
//             return new State(this);
//         }

//         public class State:OnlineEntity.EntityData.EntityDataState
//         {
//             [OnlineField]
//             public bool isBomb;
//             private PotatoData potatoData;

//             public State(PotatoData potatoData)
//             {
//                 isBomb = potatoData.isBomb;
//             }


//             public override Type GetDataType()=>typeof(PotatoData);

//             public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
//             {
//                 var potatoData = (PotatoData)data;
//                 potatoData.isBomb = isBomb;
//             }

//         }

//     }
// }