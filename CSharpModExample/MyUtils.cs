namespace CSharpExample;

public static class MyUtils
{
    private static UWorld? _world;

    public static UWorld? GetWorld()
    {
        if (_world == null)
        {
            var uobjectRef = GCHelper.FindRef(FGlobals.GWorld);
            _world = uobjectRef?.Managed as UWorld;
        }

        return _world;
    }

    public static APawn? GetControlledPawn() => UGSE_EngineFuncLib.GetFirstLocalPlayerController(GetWorld()).GetControlledPawn();

    public static BGUPlayerCharacterCS GetBGUPlayerCharacterCS() => (GetControlledPawn() as BGUPlayerCharacterCS)!;

    public static BGP_PlayerControllerB1 GetPlayerController() => (BGP_PlayerControllerB1)UGSE_EngineFuncLib.GetFirstLocalPlayerController(GetWorld());

    public static BUS_GSEventCollection GetBUS_GSEventCollection() => BUS_EventCollectionCS.Get(GetControlledPawn());

    public static T? LoadAsset<T>(string asset) where T : UObject =>
        BGW_PreloadAssetMgr.Get(GetWorld()).TryGetCachedResourceObj<T>(asset, ELoadResourceType.SyncLoadAndCache);

    public static UClass? LoadClass(string asset) => LoadAsset<UClass>(asset);

    public static AActor? SpawnActor(string classAsset)
    {
        var controlledPawn = GetControlledPawn();
        if (controlledPawn == null)
        {
            return null;
        }

        var actorLocation = controlledPawn.GetActorLocation();
        var b = controlledPawn.GetActorForwardVector() * 1000.0f;
        var start = actorLocation + b;
        var fRotator = UMathLibrary.FindLookAtRotation(start, actorLocation);
        var uClass = LoadClass($"PrefabricatorAsset'{classAsset}'");
        if (uClass == null)
        {
            return null;
        }

        return BGUFunctionLibraryCS.BGUSpawnActor(controlledPawn.World, uClass, start, fRotator);
    }

    public static AActor GetActorOfClass(string classAsset) => UGameplayStatics.GetActorOfClass(GetWorld(), LoadAsset<UClass>(classAsset));
}