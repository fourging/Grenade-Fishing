# Item对象详解

## 概述

Item类是《逃离鸭科夫》游戏中最基础的对象，所有物品都继承自此类。它是一个MonoBehaviour组件，负责管理游戏中的所有物品属性、行为和交互。

## 核心属性

### 基本属性

#### TypeID
- **类型**: `int`
- **描述**: 物品的唯一类型标识符，用于区分不同类型的物品
- **访问方式**: `item.TypeID`
- **示例**: 
```csharp
int typeId = item.TypeID;
Debug.Log($"物品类型ID: {typeId}");
```

#### DisplayName
- **类型**: `string` (只读)
- **描述**: 物品的显示名称，经过本地化处理后的纯文本
- **访问方式**: `item.DisplayName`
- **示例**:
```csharp
string name = item.DisplayName;
Debug.Log($"物品名称: {name}");
```

#### DisplayNameRaw
- **类型**: `string`
- **描述**: 物品的原始显示名称键值，用于本地化系统
- **访问方式**: `item.DisplayNameRaw`
- **示例**:
```csharp
string rawName = item.DisplayNameRaw;
Debug.Log($"物品原始名称键: {rawName}");
```

#### Description
- **类型**: `string` (只读)
- **描述**: 物品的描述文本，经过本地化处理后的纯文本
- **访问方式**: `item.Description`
- **示例**:
```csharp
string description = item.Description;
Debug.Log($"物品描述: {description}");
```

#### Icon
- **类型**: `Sprite`
- **描述**: 物品的图标
- **访问方式**: `item.Icon`
- **示例**:
```csharp
Sprite icon = item.Icon;
// 用于UI显示
itemIconImage.sprite = icon;
```

### 数值属性

#### Value
- **类型**: `int`
- **描述**: 物品的基础价值
- **访问方式**: `item.Value`
- **示例**:
```csharp
int value = item.Value;
Debug.Log($"物品价值: {value}");
```

#### Quality
- **类型**: `int`
- **描述**: 物品品质等级
- **访问方式**: `item.Quality`
- **示例**:
```csharp
int quality = item.Quality;
Debug.Log($"物品品质: {quality}");
```

#### DisplayQuality
- **类型**: `DisplayQuality`
- **描述**: 物品的显示品质枚举
- **访问方式**: `item.DisplayQuality`
- **示例**:
```csharp
DisplayQuality displayQuality = item.DisplayQuality;
Debug.Log($"物品显示品质: {displayQuality}");
```

### 堆叠属性

#### MaxStackCount
- **类型**: `int`
- **描述**: 物品的最大堆叠数量
- **访问方式**: `item.MaxStackCount`
- **示例**:
```csharp
int maxStack = item.MaxStackCount;
Debug.Log($"最大堆叠数量: {maxStack}");
```

#### Stackable
- **类型**: `bool` (只读)
- **描述**: 物品是否可堆叠
- **访问方式**: `item.Stackable`
- **示例**:
```csharp
bool canStack = item.Stackable;
if (canStack) {
    Debug.Log("物品可堆叠");
}
```

#### StackCount
- **类型**: `int`
- **描述**: 物品的当前堆叠数量
- **访问方式**: `item.StackCount`
- **示例**:
```csharp
int currentStack = item.StackCount;
Debug.Log($"当前堆叠数量: {currentStack}");

// 设置堆叠数量
item.StackCount = 5;
```

### 重量属性

#### UnitSelfWeight
- **类型**: `float` (只读)
- **描述**: 单个物品的重量
- **访问方式**: `item.UnitSelfWeight`
- **示例**:
```csharp
float unitWeight = item.UnitSelfWeight;
Debug.Log($"单个物品重量: {unitWeight}");
```

#### SelfWeight
- **类型**: `float` (只读)
- **描述**: 物品自身的总重量（单个重量 × 堆叠数量）
- **访问方式**: `item.SelfWeight`
- **示例**:
```csharp
float selfWeight = item.SelfWeight;
Debug.Log($"物品自身总重量: {selfWeight}");
```

#### TotalWeight
- **类型**: `float` (只读)
- **描述**: 物品的总重量（包括子物品和背包内容）
- **访问方式**: `item.TotalWeight`
- **示例**:
```csharp
float totalWeight = item.TotalWeight;
Debug.Log($"物品总重量: {totalWeight}");
```

### 耐久度属性

#### MaxDurability
- **类型**: `float`
- **描述**: 物品的最大耐久度
- **访问方式**: `item.MaxDurability`
- **示例**:
```csharp
float maxDurability = item.MaxDurability;
Debug.Log($"最大耐久度: {maxDurability}");

// 设置最大耐久度
item.MaxDurability = 100f;
```

#### Durability
- **类型**: `float`
- **描述**: 物品的当前耐久度
- **访问方式**: `item.Durability`
- **示例**:
```csharp
float durability = item.Durability;
Debug.Log($"当前耐久度: {durability}");

// 设置耐久度
item.Durability = 75f;
```

#### UseDurability
- **类型**: `bool` (只读)
- **描述**: 物品是否使用耐久度系统
- **访问方式**: `item.UseDurability`
- **示例**:
```csharp
bool usesDurability = item.UseDurability;
if (usesDurability) {
    Debug.Log("物品使用耐久度系统");
}
```

#### Repairable
- **类型**: `bool` (只读)
- **描述**: 物品是否可修复
- **访问方式**: `item.Repairable`
- **示例**:
```csharp
bool canRepair = item.Repairable;
if (canRepair) {
    Debug.Log("物品可修复");
}
```

### 标签系统

#### Tags
- **类型**: `TagCollection`
- **描述**: 物品的标签集合，用于分类和过滤
- **访问方式**: `item.Tags`
- **示例**:
```csharp
TagCollection tags = item.Tags;
foreach (var tag in tags) {
    Debug.Log($"物品标签: {tag.DisplayName}");
}

// 检查是否包含特定标签
bool hasWeaponTag = item.Tags.Contains("Weapon");
```

#### Sticky
- **类型**: `bool` (只读)
- **描述**: 物品是否为粘性物品（不可丢弃或出售）
- **访问方式**: `item.Sticky`
- **示例**:
```csharp
bool isSticky = item.Sticky;
if (isSticky) {
    Debug.Log("物品为粘性物品，不可丢弃或出售");
}
```

#### CanBeSold
- **类型**: `bool` (只读)
- **描述**: 物品是否可以出售
- **访问方式**: `item.CanBeSold`
- **示例**:
```csharp
bool canSell = item.CanBeSold;
if (canSell) {
    Debug.Log("物品可以出售");
}
```

#### CanDrop
- **类型**: `bool` (只读)
- **描述**: 物品是否可以丢弃
- **访问方式**: `item.CanDrop`
- **示例**:
```csharp
bool canDrop = item.CanDrop;
if (canDrop) {
    Debug.Log("物品可以丢弃");
}
```

### 变量系统

#### Variables
- **类型**: `CustomDataCollection`
- **描述**: 物品的自定义变量集合，用于存储运行时数据
- **访问方式**: `item.Variables`
- **示例**:
```csharp
// 获取变量
float customValue = item.GetFloat("CustomValue", 0f);
int customCount = item.GetInt("CustomCount", 0);
bool customFlag = item.GetBool("CustomFlag", false);
string customText = item.GetString("CustomText", "");

// 设置变量
item.SetFloat("CustomValue", 10.5f);
item.SetInt("CustomCount", 5);
item.SetBool("CustomFlag", true);
item.SetString("CustomText", "Hello World");
```

#### Constants
- **类型**: `CustomDataCollection`
- **描述**: 物品的常量集合，用于存储设计时数据
- **访问方式**: `item.Constants`
- **示例**:
```csharp
// 获取常量
float maxDurability = item.Constants.GetFloat("MaxDurability", 0f);
string itemCategory = item.Constants.GetString("Category", "");
```

### 组件系统

#### Stats
- **类型**: `StatCollection`
- **描述**: 物品的统计属性集合
- **访问方式**: `item.Stats`
- **示例**:
```csharp
StatCollection stats = item.Stats;
if (stats != null) {
    // 获取特定统计属性
    Stat damageStat = stats.GetStat("Damage");
    if (damageStat != null) {
        float damage = damageStat.Value;
        Debug.Log($"伤害值: {damage}");
    }
    
    // 直接获取统计属性值
    float defense = item.GetStatValue("Defense");
    Debug.Log($"防御值: {defense}");
}
```

#### Slots
- **类型**: `SlotCollection`
- **描述**: 物品的插槽集合
- **访问方式**: `item.Slots`
- **示例**:
```csharp
SlotCollection slots = item.Slots;
if (slots != null) {
    foreach (var slot in slots) {
        if (slot.Content != null) {
            Debug.Log($"插槽 {slot.Key} 中有物品: {slot.Content.DisplayName}");
        }
    }
}
```

#### Modifiers
- **类型**: `ModifierDescriptionCollection`
- **描述**: 物品的修饰符集合
- **访问方式**: `item.Modifiers`
- **示例**:
```csharp
ModifierDescriptionCollection modifiers = item.Modifiers;
if (modifiers != null) {
    Debug.Log($"物品有 {modifiers.Count} 个修饰符");
}
```

#### Inventory
- **类型**: `Inventory`
- **描述**: 物品的背包组件（用于容器类物品）
- **访问方式**: `item.Inventory`
- **示例**:
```csharp
Inventory inventory = item.Inventory;
if (inventory != null) {
    Debug.Log($"物品有背包，容量: {inventory.Capacity}");
    
    // 遍历背包内容
    foreach (var invItem in inventory) {
        Debug.Log($"背包中有物品: {invItem.DisplayName}");
    }
}
```

#### Effects
- **类型**: `List<Effect>`
- **描述**: 物品的效果列表
- **访问方式**: `item.Effects`
- **示例**:
```csharp
List<Effect> effects = item.Effects;
if (effects != null && effects.Count > 0) {
    Debug.Log($"物品有 {effects.Count} 个效果");
    
    // 添加新效果
    Effect newEffect = CreateCustomEffect();
    item.AddEffect(newEffect);
}
```

### 状态属性

#### Inspected
- **类型**: `bool`
- **描述**: 物品是否已被检查
- **访问方式**: `item.Inspected`
- **示例**:
```csharp
bool inspected = item.Inspected;
if (!inspected) {
    Debug.Log("物品尚未检查");
    item.Inspected = true;
}
```

#### Inspecting
- **类型**: `bool`
- **描述**: 物品是否正在被检查
- **访问方式**: `item.Inspecting`
- **示例**:
```csharp
bool inspecting = item.Inspecting;
if (inspecting) {
    Debug.Log("物品正在被检查");
}
```

#### NeedInspection
- **类型**: `bool` (只读)
- **描述**: 物品是否需要检查
- **访问方式**: `item.NeedInspection`
- **示例**:
```csharp
bool needInspection = item.NeedInspection;
if (needInspection) {
    Debug.Log("物品需要检查");
}
```

### 关系属性

#### ParentItem
- **类型**: `Item` (只读)
- **描述**: 物品的父级物品
- **访问方式**: `item.ParentItem`
- **示例**:
```csharp
Item parent = item.ParentItem;
if (parent != null) {
    Debug.Log($"物品的父级是: {parent.DisplayName}");
}
```

#### ParentObject
- **类型**: `UnityEngine.Object` (只读)
- **描述**: 物品的父级对象（可能是Item或Inventory）
- **访问方式**: `item.ParentObject`
- **示例**:
```csharp
UnityEngine.Object parent = item.ParentObject;
if (parent is Item parentItem) {
    Debug.Log($"父级是物品: {parentItem.DisplayName}");
} else if (parent is Inventory parentInventory) {
    Debug.Log("父级是背包");
}
```

#### PluggedIntoSlot
- **类型**: `Slot` (只读)
- **描述**: 物品插入的插槽
- **访问方式**: `item.PluggedIntoSlot`
- **示例**:
```csharp
Slot slot = item.PluggedIntoSlot;
if (slot != null) {
    Debug.Log($"物品插入在插槽: {slot.Key}");
}
```

#### InInventory
- **类型**: `Inventory` (只读)
- **描述**: 物品所在的背包
- **访问方式**: `item.InInventory`
- **示例**:
```csharp
Inventory inventory = item.InInventory;
if (inventory != null) {
    Debug.Log("物品在背包中");
}
```

## 常用方法

### 物品操作

#### Use(object user)
- **描述**: 使用物品
- **参数**: `user` - 使用物品的对象
- **示例**:
```csharp
item.Use(player);
```

#### IsUsable(object user)
- **描述**: 检查物品是否可以被指定对象使用
- **参数**: `user` - 要检查的对象
- **返回值**: `bool` - 是否可用
- **示例**:
```csharp
bool canUse = item.IsUsable(player);
if (canUse) {
    item.Use(player);
}
```

#### CreateInstance()
- **描述**: 创建物品的实例
- **返回值**: `Item` - 新创建的物品实例
- **示例**:
```csharp
Item newItem = item.CreateInstance();
```

#### Detach()
- **描述**: 从当前父级（插槽或背包）中分离物品
- **示例**:
```csharp
item.Detach();
```

### 堆叠操作

#### Combine(Item incomingItem)
- **描述**: 将另一个物品合并到当前物品中
- **参数**: `incomingItem` - 要合并的物品
- **示例**:
```csharp
item.Combine(otherItem);
```

#### CombineInto(Item otherItem)
- **描述**: 将当前物品合并到另一个物品中
- **参数**: `otherItem` - 目标物品
- **示例**:
```csharp
item.CombineInto(otherItem);
```

#### Split(int count)
- **描述**: 分割物品，创建指定数量的新物品
- **参数**: `count` - 要分割的数量
- **返回值**: `UniTask<Item>` - 异步返回新物品
- **示例**:
```csharp
Item splitItem = await item.Split(5);
```

### 变量操作

#### GetVariableEntry(string variableKey)
- **描述**: 获取指定键的变量条目
- **参数**: `variableKey` - 变量键
- **返回值**: `CustomData` - 变量条目
- **示例**:
```csharp
CustomData entry = item.GetVariableEntry("CustomValue");
if (entry != null) {
    Debug.Log($"变量值: {entry.Value}");
}
```

#### GetFloat/GetInt/GetBool/GetString
- **描述**: 获取指定类型的变量值
- **参数**: 
  - `key` - 变量键或哈希值
  - `defaultResult` - 默认值（可选）
- **返回值**: 对应类型的值
- **示例**:
```csharp
float floatValue = item.GetFloat("FloatKey", 0f);
int intValue = item.GetInt("IntKey", 0);
bool boolValue = item.GetBool("BoolKey", false);
string stringValue = item.GetString("StringKey", "");
```

#### SetFloat/SetInt/SetBool/SetString
- **描述**: 设置指定类型的变量值
- **参数**: 
  - `key` - 变量键或哈希值
  - `value` - 要设置的值
  - `createNewIfNotExist` - 如果不存在是否创建新条目（可选，默认true）
- **示例**:
```csharp
item.SetFloat("FloatKey", 10.5f);
item.SetInt("IntKey", 5);
item.SetBool("BoolKey", true);
item.SetString("StringKey", "Hello World");
```

### 统计属性操作

#### GetStat(string key) / GetStat(int hash)
- **描述**: 获取指定键的统计属性
- **参数**: `key` - 统计属性键或哈希值
- **返回值**: `Stat` - 统计属性对象
- **示例**:
```csharp
Stat damageStat = item.GetStat("Damage");
if (damageStat != null) {
    Debug.Log($"伤害值: {damageStat.Value}");
}
```

#### GetStatValue(string key) / GetStatValue(int hash)
- **描述**: 获取指定键的统计属性值
- **参数**: `key` - 统计属性键或哈希值
- **返回值**: `float` - 统计属性值
- **示例**:
```csharp
float damage = item.GetStatValue("Damage");
float defense = item.GetStatValue("Defense");
```

#### AddModifier(string statKey, Modifier modifier)
- **描述**: 为指定统计属性添加修饰符
- **参数**: 
  - `statKey` - 统计属性键
  - `modifier` - 修饰符对象
- **返回值**: `bool` - 是否添加成功
- **示例**:
```csharp
Modifier damageModifier = new Modifier(10f, ModifierType.Add, this);
bool success = item.AddModifier("Damage", damageModifier);
```

### 价值计算

#### GetTotalRawValue()
- **描述**: 计算物品的总价值（考虑耐久度、堆叠数量和子物品）
- **返回值**: `int` - 总价值
- **示例**:
```csharp
int totalValue = item.GetTotalRawValue();
Debug.Log($"物品总价值: {totalValue}");
```

### 重量计算

#### RecalculateTotalWeight()
- **描述**: 重新计算物品的总重量
- **返回值**: `float` - 总重量
- **示例**:
```csharp
float totalWeight = item.RecalculateTotalWeight();
Debug.Log($"重新计算的总重量: {totalWeight}");
```

## 常用事件

### 基本事件

#### onDestroy
- **描述**: 物品销毁时触发
- **参数**: `Item` - 被销毁的物品
- **示例**:
```csharp
item.onDestroy += OnItemDestroyed;

private void OnItemDestroyed(Item destroyedItem) {
    Debug.Log($"物品 {destroyedItem.DisplayName} 已销毁");
}
```

#### onUse
- **描述**: 物品使用时触发
- **参数**: `Item` - 被使用的物品, `object` - 使用者
- **示例**:
```csharp
item.onUse += OnItemUsed;

private void OnItemUsed(Item usedItem, object user) {
    Debug.Log($"物品 {usedItem.DisplayName} 被 {user} 使用");
}
```

#### onUseStatic
- **描述**: 任何物品使用时触发的静态事件
- **参数**: `Item` - 被使用的物品, `object` - 使用者
- **示例**:
```csharp
Item.onUseStatic += OnAnyItemUsed;

private void OnAnyItemUsed(Item usedItem, object user) {
    Debug.Log($"物品 {usedItem.DisplayName} 被 {user} 使用");
}
```

### 状态变化事件

#### onDurabilityChanged
- **描述**: 物品耐久度变化时触发
- **参数**: `Item` - 耐久度变化的物品
- **示例**:
```csharp
item.onDurabilityChanged += OnDurabilityChanged;

private void OnDurabilityChanged(Item changedItem) {
    Debug.Log($"物品 {changedItem.DisplayName} 耐久度变化为 {changedItem.Durability}");
}
```

#### onSetStackCount
- **描述**: 物品堆叠数量设置时触发
- **参数**: `Item` - 堆叠数量变化的物品
- **示例**:
```csharp
item.onSetStackCount += OnStackCountChanged;

private void OnStackCountChanged(Item changedItem) {
    Debug.Log($"物品 {changedItem.DisplayName} 堆叠数量变化为 {changedItem.StackCount}");
}
```

#### onInspectionStateChanged
- **描述**: 物品检查状态变化时触发
- **参数**: `Item` - 检查状态变化的物品
- **示例**:
```csharp
item.onInspectionStateChanged += OnInspectionStateChanged;

private void OnInspectionStateChanged(Item changedItem) {
    Debug.Log($"物品 {changedItem.DisplayName} 检查状态变化");
}
```

### 关系变化事件

#### onParentChanged
- **描述**: 物品父级变化时触发
- **参数**: `Item` - 父级变化的物品
- **示例**:
```csharp
item.onParentChanged += OnParentChanged;

private void OnParentChanged(Item changedItem) {
    Debug.Log($"物品 {changedItem.DisplayName} 父级变化");
}
```

#### onPluggedIntoSlot
- **描述**: 物品插入插槽时触发
- **参数**: `Item` - 被插入的物品
- **示例**:
```csharp
item.onPluggedIntoSlot += OnPluggedIntoSlot;

private void OnPluggedIntoSlot(Item pluggedItem) {
    Debug.Log($"物品 {pluggedItem.DisplayName} 被插入插槽");
}
```

#### onUnpluggedFromSlot
- **描述**: 物品从插槽拔出时触发
- **参数**: `Item` - 被拔出的物品
- **示例**:
```csharp
item.onUnpluggedFromSlot += OnUnpluggedFromSlot;

private void OnUnpluggedFromSlot(Item unpluggedItem) {
    Debug.Log($"物品 {unpluggedItem.DisplayName} 从插槽拔出");
}
```

### 结构变化事件

#### onItemTreeChanged
- **描述**: 物品树结构变化时触发
- **参数**: `Item` - 变化的物品
- **示例**:
```csharp
item.onItemTreeChanged += OnItemTreeChanged;

private void OnItemTreeChanged(Item changedItem) {
    Debug.Log($"物品 {changedItem.DisplayName} 树结构变化");
}
```

#### onChildChanged
- **描述**: 物品子级变化时触发
- **参数**: `Item` - 子级变化的物品
- **示例**:
```csharp
item.onChildChanged += OnChildChanged;

private void OnChildChanged(Item changedItem) {
    Debug.Log($"物品 {changedItem.DisplayName} 子级变化");
}
```

#### onSlotContentChanged
- **描述**: 物品插槽内容变化时触发
- **参数**: `Item` - 插槽内容变化的物品, `Slot` - 变化的插槽
- **示例**:
```csharp
item.onSlotContentChanged += OnSlotContentChanged;

private void OnSlotContentChanged(Item changedItem, Slot changedSlot) {
    Debug.Log($"物品 {changedItem.DisplayName} 插槽 {changedSlot.Key} 内容变化");
}
```

## 使用示例

### 创建和配置物品

```csharp
// 创建物品实例
Item item = ItemAssetsCollection.InstantiateSync(itemTypeId);

if (item != null) {
    // 设置基本属性
    item.DisplayNameRaw = "CustomItem";
    item.Value = 100;
    item.Quality = 2;
    item.MaxStackCount = 10;
    
    // 设置重量
    item.weight = 2.5f;
    
    // 设置耐久度
    item.MaxDurability = 100f;
    item.Durability = 100f;
    
    // 添加标签
    item.Tags.Add("Weapon");
    item.Tags.Add("Rare");
    
    // 设置自定义变量
    item.SetFloat("Damage", 25f);
    item.SetInt("Level", 5);
    item.SetBool("Enchanted", true);
    
    // 添加统计属性
    if (item.Stats != null) {
        item.Stats.AddStat("Damage", 25f);
        item.Stats.AddStat("AttackSpeed", 1.2f);
    }
    
    // 添加效果
    Effect fireEffect = CreateFireEffect();
    item.AddEffect(fireEffect);
    
    Debug.Log($"物品 {item.DisplayName} 创建并配置完成");
}
```

### 监听物品事件

```csharp
void Start() {
    // 监听物品使用事件
    Item.onUseStatic += OnAnyItemUsed;
    
    // 监听特定物品的事件
    if (targetItem != null) {
        targetItem.onDestroy += OnItemDestroyed;
        targetItem.onDurabilityChanged += OnDurabilityChanged;
        targetItem.onParentChanged += OnParentChanged;
    }
}

void OnDestroy() {
    // 取消监听事件
    Item.onUseStatic -= OnAnyItemUsed;
    
    if (targetItem != null) {
        targetItem.onDestroy -= OnItemDestroyed;
        targetItem.onDurabilityChanged -= OnDurabilityChanged;
        targetItem.onParentChanged -= OnParentChanged;
    }
}

private void OnAnyItemUsed(Item usedItem, object user) {
    Debug.Log($"物品 {usedItem.DisplayName} 被 {user} 使用");
}

private void OnItemDestroyed(Item destroyedItem) {
    Debug.Log($"物品 {destroyedItem.DisplayName} 已销毁");
}

private void OnDurabilityChanged(Item changedItem) {
    float durabilityPercent = (changedItem.Durability / changedItem.MaxDurability) * 100f;
    Debug.Log($"物品 {changedItem.DisplayName} 耐久度: {durabilityPercent:F1}%");
    
    // 耐久度过低时显示警告
    if (durabilityPercent < 20f) {
        Debug.LogWarning($"警告: {changedItem.DisplayName} 耐久度过低!");
    }
}

private void OnParentChanged(Item changedItem) {
    Debug.Log($"物品 {changedItem.DisplayName} 父级关系变化");
    
    // 检查物品的新位置
    if (changedItem.PluggedIntoSlot != null) {
        Debug.Log($"物品现在在插槽: {changedItem.PluggedIntoSlot.Key}");
    } else if (changedItem.InInventory != null) {
        Debug.Log("物品现在在背包中");
    } else {
        Debug.Log("物品现在在世界中");
    }
}
```

### 物品操作示例

```csharp
void ProcessItem(Item item) {
    if (item == null) return;
    
    // 检查物品是否可用
    if (item.IsUsable(player)) {
        item.Use(player);
    }
    
    // 检查耐久度
    if (item.UseDurability && item.Durability <= 0) {
        Debug.Log($"物品 {item.DisplayName} 耐久度耗尽，无法使用");
        return;
    }
    
    // 检查是否需要修复
    if (item.Repairable && item.Durability < item.MaxDurability * 0.5f) {
        Debug.Log($"物品 {item.DisplayName} 需要修复");
        // 修复逻辑...
    }
    
    // 处理堆叠
    if (item.Stackable && item.StackCount > 1) {
        Debug.Log($"物品 {item.DisplayName} 有 {item.StackCount} 个");
        
        // 如果数量过多，分割物品
        if (item.StackCount > item.MaxStackCount / 2) {
            SplitItem(item, item.StackCount / 2);
        }
    }
    
    // 计算总价值
    int totalValue = item.GetTotalRawValue();
    Debug.Log($"物品 {item.DisplayName} 总价值: {totalValue}");
    
    // 计算总重量
    float totalWeight = item.TotalWeight;
    Debug.Log($"物品 {item.DisplayName} 总重量: {totalWeight}");
}

async void SplitItem(Item item, int count) {
    Item splitItem = await item.Split(count);
    if (splitItem != null) {
        Debug.Log($"成功分割出 {count} 个 {splitItem.DisplayName}");
        
        // 将分割的物品添加到背包
        if (playerInventory != null) {
            playerInventory.AddItem(splitItem);
        }
    }
}
```

## 注意事项

1. **TypeID唯一性**: 每个物品类型的TypeID必须唯一，避免冲突
2. **堆叠限制**: 可堆叠物品不应包含Slot、Inventory等独特信息
3. **变量管理**: 可堆叠物品通常只应包含Count变量
4. **耐久度系统**: 耐久度为0时，某些效果可能不会生效
5. **事件监听**: 记得在适当的时候取消事件监听，避免内存泄漏
6. **重量计算**: TotalWeight是递归计算的，包含所有子物品和背包内容的重量
7. **价值计算**: GetTotalRawValue考虑了耐久度、堆叠数量和子物品的价值
8. **标签使用**: 合理使用标签系统进行物品分类和过滤
9. **父子关系**: 物品的父子关系会影响其效果和重量计算
10. **异步操作**: Split等方法是异步的，需要使用await处理

## 总结

Item对象是《逃离鸭科夫》中物品系统的核心，提供了丰富的属性和方法来管理物品的各种特性。通过合理使用这些属性和方法，可以实现复杂的物品交互逻辑，包括物品的创建、配置、使用、堆叠、分割、价值计算等功能。同时，事件系统允许我们监听和响应物品的各种状态变化，为游戏逻辑提供了强大的扩展能力。