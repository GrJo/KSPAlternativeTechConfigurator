using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ATC
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    class TechChanger : MonoBehaviour
    {
        bool loadOnNextUpdate = false;

        static bool bIsInstantiated = false;
        static bool bRemoveEventsOnDestroy = true;

        ConfigNode settings = new ConfigNode();
        ConfigNode tree = new ConfigNode();

        void Start()
        {
            settings = ConfigNode.Load("GameData/ATC/settings.cfg");
            tree = ConfigNode.Load(settings.GetValue("TechTree"));

            if (!tree.HasData)
            {
                Debug.LogError("TechChanger: Treeconfig '" + settings.GetValue("TechTree") + "' empty/not found!");
                return;
            }

            setupBodyScienceParams();

            if (!bIsInstantiated)
            {
                GameEvents.onGUIRnDComplexSpawn.Add(new EventVoid.OnEvent(OnGUIRnDComplexSpawn));
                GameEvents.onGUIRnDComplexDespawn.Add(new EventVoid.OnEvent(OnGUIRnDComplexDespawn));
                DontDestroyOnLoad(this);

                bIsInstantiated = true;
            }
            else
            {
                bRemoveEventsOnDestroy = false;

                Destroy(this);
            }
        }

        void OnDestroy()
        {
            if (bRemoveEventsOnDestroy)
            {
                GameEvents.onGUIRnDComplexSpawn.Remove(new EventVoid.OnEvent(OnGUIRnDComplexSpawn));
                GameEvents.onGUIRnDComplexDespawn.Remove(new EventVoid.OnEvent(OnGUIRnDComplexDespawn));
            }

            bRemoveEventsOnDestroy = true;
        }

        public void Update()
        {
            if (loadOnNextUpdate)
            {
                LoadTree();
            }
        }

        void OnGUIRnDComplexSpawn()
        {
            Debug.Log("RDComplexSpawn - loading tree");
            //print( "TinkerTechMonoBehaviorTechTreeMod: RnDComplexSpawn" );
            loadOnNextUpdate = true;
        }

        void OnGUIRnDComplexDespawn()
        {
        }

        private void LoadTree()
        {
            
            if (!this.tree.HasData)
            {
                Debug.LogError("TechChanger: Treeconfig '" + settings.GetValue("TechTree") + "' empty/not found!");
                return;
            }

            //foreach (AvailablePart debugPart in PartLoader.LoadedPartsList)
            //    Debug.Log("Loaded AvailablePart: " + debugPart.name);

            Debug.Log("Loading Dictionary namenodemap");
            Dictionary<string, ConfigNode> nodeNameMap = new Dictionary<string,ConfigNode>();

            foreach (ConfigNode cfgNode in tree.GetNodes("MODIFY_NODE"))
            {
                string goName = cfgNode.GetValue("gameObjectName");
                if (goName.Length > 0)
                    nodeNameMap.Add(goName, cfgNode);
            }

            
            foreach (RDNode treeNode in GameObject.FindObjectsOfType(typeof(RDNode)).Cast<RDNode>())
            {
                Debug.Log("processing treeNode " + treeNode.gameObject.name);
                //get corresponding confignode from tree.cfg
                ConfigNode cfgNode;
                if (nodeNameMap.TryGetValue(treeNode.gameObject.name, out cfgNode))
                {
                    Debug.Log("found MODIFY_NODE for treeNode " + treeNode.gameObject.name);

                    Debug.Log("checking title");
                    if (cfgNode.HasValue("title"))
                        treeNode.name = cfgNode.GetValue("title");

                    Debug.Log("checking description");
                    if (cfgNode.HasValue("description"))
                        treeNode.description = cfgNode.GetValue("description");

                    Debug.Log("checking scienceCost");
                    if (cfgNode.HasValue("scienceCost"))
                        treeNode.tech.scienceCost = int.Parse(cfgNode.GetValue("scienceCost"));

                    Debug.Log("checking icon");
                    if (cfgNode.HasValue("icon"))
                    {
                        //bool success = Enum.TryParse<RDNode.Icon>(cfgNode.GetValue("icon"), out icon); //.NET >= 4.0
                        try
                        {
                            RDNode.Icon icon = (RDNode.Icon)Enum.Parse(typeof(RDNode.Icon), cfgNode.GetValue("icon"));
                            treeNode.icon = icon;
                            treeNode.SetIconState(icon);

                        }catch(Exception ex)
                        {
                            Debug.LogError("Invalid Icon name '" + cfgNode.GetValue("icon") + "'" + ex.Message);
                            continue;
                        }
  
                    }

                    Debug.Log("checking anyParentUnlocks");
                    if (cfgNode.HasValue("anyParentUnlocks"))
                        treeNode.AnyParentToUnlock = cfgNode.GetValue("anyParentUnlocks") == "true";


                    //now get all parts
                    Debug.Log("checking PARTS");
                    if (cfgNode.HasNode("PARTS"))
                    {
                        treeNode.tech.partsAssigned.Clear();

                        ConfigNode partsNode = cfgNode.GetNode("PARTS");
                        Debug.Log("subnode PARTS has " + partsNode.GetValues("part").Count() + " part entries");

                        foreach (string partString in partsNode.GetValues("part"))
                        {

                            Debug.Log("searching part " + partString + ", internalizedName= " + internalizeName(partString));

                            AvailablePart part = PartLoader.LoadedPartsList.Find(x => x.name == internalizeName(partString));
                            try
                            {
                                Debug.Log("found AvailablePart " + part.title + " with techRequired = " + part.TechRequired + ", changing to " + treeNode.tech.techID);

                                part.TechRequired = treeNode.tech.techID;
                                treeNode.tech.partsAssigned.Add(part);
                            }
                            catch (Exception)
                            { 
                                Debug.LogWarning("ATC: part " + partString + " does not exist, skipping it!");
                            }
                            
                        }
                    }//endif has "PARTS" subnode

                    treeNode.UpdateGraphics();
                }//endif node found
            }//end for all nodes;
        } //end loadTree()


        private void setupBodyScienceParams()
        {
            if (!this.tree.HasData)
            {
                Debug.LogError("TechChanger: Treeconfig '" + settings.GetValue("TechTree") + "' empty/not found!");
                return;
            }

            foreach (ConfigNode scienceParamsNode in tree.GetNodes("BODY_SCIENCE_PARAMS"))
            {
                string bodyName = scienceParamsNode.GetValue("celestialBody");
                CelestialBody body = FlightGlobals.Bodies.Find(x => x.name == bodyName);

                try
                {
                    Debug.Log("ATC: Modifying celestialBody science params for " + bodyName);
                    //Science value factors
                    if (scienceParamsNode.HasValue("LandedDataValue"))
                        body.scienceValues.LandedDataValue = float.Parse(scienceParamsNode.GetValue("LandedDataValue"));
                    if (scienceParamsNode.HasValue("SplashedDataValue"))
                        body.scienceValues.SplashedDataValue = float.Parse(scienceParamsNode.GetValue("SplashedDataValue"));
                    if (scienceParamsNode.HasValue("FlyingLowDataValue"))
                        body.scienceValues.FlyingLowDataValue = float.Parse(scienceParamsNode.GetValue("FlyingLowDataValue"));
                    if (scienceParamsNode.HasValue("FlyingHighDataValue"))
                        body.scienceValues.FlyingHighDataValue = float.Parse(scienceParamsNode.GetValue("FlyingHighDataValue"));
                    if (scienceParamsNode.HasValue("InSpaceLowDataValue"))
                        body.scienceValues.InSpaceLowDataValue = float.Parse(scienceParamsNode.GetValue("InSpaceLowDataValue"));
                    if (scienceParamsNode.HasValue("InSpaceHighDataValue"))
                        body.scienceValues.InSpaceHighDataValue = float.Parse(scienceParamsNode.GetValue("InSpaceHighDataValue"));
                    if (scienceParamsNode.HasValue("RecoveryValue"))
                        body.scienceValues.RecoveryValue = float.Parse(scienceParamsNode.GetValue("RecoveryValue"));

                    //Zone thresholds

                    if (scienceParamsNode.HasValue("flyingAltitudeThreshold"))
                        body.scienceValues.flyingAltitudeThreshold = float.Parse(scienceParamsNode.GetValue("flyingAltitudeThreshold"));
                    if (scienceParamsNode.HasValue("FlyingHighDataValue"))
                        body.scienceValues.spaceAltitudeThreshold = float.Parse(scienceParamsNode.GetValue("spaceAltitudeThreshold"));
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            }
        }

        private string internalizeName(string partName)
        {
            return partName.Replace("_", ".");
        }
    }



}



