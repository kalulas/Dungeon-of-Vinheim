# Dungeon-of-Vinheim

彼海姆的地下城（暂定）

一个主人与侵入者对战的3D动作游戏，含一定Rogue-like要素（这个也是暂定）

后期会在这里补充游戏的玩法介绍，操作指南，地图设计怪物设计UI设计等等

工作进展我就还是自己写一点sprint会议然后一边用GitHub的project来跟踪

### 场景设计

#### 基础房间

7*7 瓦片地板大小，中央容器照明；四个方向都有双开门，门两边设置火把；四个角落都有柱子，柱子上也设置火把；为了光照在天花板设置了四个不投射阴影的吊灯；

#### 起始房间

生成玩家的起始房间：中央有生成法阵；法阵四周有雕塑；

#### 战斗房间

生成怪物，玩家战斗的房间；中央容器周围有棺材，看守火盆的一尊雕塑；限制房间怪物数上限为8；三到四个石头障碍物，后期试着用脚本随机生成；

#### 奖励房间

四个雕塑对应的四个宝箱，里面可以放点恢复药水强化药水之类的，前期就设定为现碰现用的那种好了，后期有时间做背包道具系统再说；可以设定宝箱之一是陷阱或者宝箱怪，地城主人可以把道具替换为陷阱等等；

#### BOSS房间

中间有BOSS的王座，根据玩家进入的入口改变方向，旁边有象征邪恶力量的四个符文柱；想做的夸张一点，场景大概要做成可破坏不然太占空间，目前就只是随便摆了几个骷髅；

#### 其他临时想到的设定

火焰的颜色可以根据地城主人的不同操作改变颜色

玩家死亡后怪物全数复活（奖励刷不刷新看心情）

如果门后面是BOSS门的颜色会变化

### 半随机地图生成算法设计

设计思路：（一些细节可以通过后期游玩体验）

1. 所有房间生成后都放在同一个场景，同一时间仅enable一个房间，其余房间均disable（因为每个房间的内容其实不是很多又相似，类似于对象池的思想，就避免了场景切换），玩家在移动时切换房间的显示并且移动玩家角色的位置。
2. **我突然觉得利用数组下标挺好的，反正一次移动房间相当于一个元素交换**，房间类：房间编号（也可以不必了），房间类型（起始，战斗，奖励，BOSS，空洞（用于房间移动）），房间GameObject对象（可以只初始化一个基础房间GameObject和四个房间ExtraGameObject），~~出入口GameObject列表~~（得保留位置用于玩家切换房间的位置加载），房间怪物、道具箱GameObject列表，~~房间灯光GameObject列表~~（后期用于脚本控制的一些对象）*出入口和房间灯光可以所有房间用同一组GameObject，直接作为GameManager成员就好了，见下一条*
3. （可以只初始化一个基础房间GameObject和四个房间ExtraGameObject）解释：场景的基础房间GameObject不变，因为墙壁地板吊灯等等都是一样的，其余不同类型的房间只要绑定多余的GameObject即可（例如战斗房间里的棺材），在进入房间时enable即可
4. 地图显示均为正方形，n*n的房间分布，具体各类房间的数量：起始，空洞，BOSS只有一个，奖励房间个数floor( n - 3/n )个 其实趋近于 n-1，直接用n-1吧。
5. 在GameManager里定义一个房间对象列表，先初始化n*n个对象，按顺序产生起始，空洞，n-1个奖励，接下来产生战斗，最后产生BOSS，然后吧第2到n-1个（从1开始）随机排列。同时在GameManager里面定义一个整型标志当前所在房间下标。
6. 侵入者在房间间移动的方法：玩家触及出入口碰撞器时Trigger判断这个方向上有没有房间（也可以在没有房间的位置打上一个叉叉），是哪个房间，如果存在房间则disable当前房间，enable目标房间，并且根据玩家的进入方向调整玩家的位置（右门朝右进去就变成左门朝右出来），BOSS房间可以特别调整一下王座的朝向问题，一般也不会二次进入（设置成死亡或者通关才能离开）。后期可以用动画或者其他什么来掩盖加载时间如果有那个必要和技术
7. 主人移动房间的方法：是类似缺一拼图的移动方式，目前的想法是所有房间都能够移动，具体什么条件下可以移动要看后面的数值策划，想要鼓励玩家多移动迷宫，所以玩家所在的房间就设计成可移动的（先玩玩看八）。代码实现就是将数组的两个对象交换，其中一个肯定是空房间，因为要消耗点数所以肯定不会经常发生，而且在移动房间时可以添加“真实的”房间移动声，把出入口全部标记“禁用”（或者显示卡住了），并且调整灯的颜色。



项目开始时间 2020 / 01 / 11