using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RoomType
{
    EmptyRoom,
    StartingRoom,
    BattleRoom,
    RewardRoom,
    BossRoom
}

public class Room
{
    // private int roomIndex;
    private RoomType roomType;
    private GameObject roomExtra;
    private List<GameObject> enemyList;
    private List<GameObject> itemList;
    
    public Room(RoomType type, GameObject extra=null, List<GameObject> enemies=null, List<GameObject> items=null){
        roomType = type;
        roomExtra = extra;
        enemyList = enemies;
        itemList = items;
    }

    public bool Empty(){
        return roomType == RoomType.EmptyRoom;
    }

    public void SetRoomActive(bool value){
        roomExtra.SetActive(value);
        if (enemyList != null)
        {
            foreach (var GameObject in enemyList)
            {
                GameObject.SetActive(value);
            }
        }
        if (itemList != null)
        {
            foreach (var GameObject in itemList)
            {
                GameObject.SetActive(value);
            }
        }
    }
}
