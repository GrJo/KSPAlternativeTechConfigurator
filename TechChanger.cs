using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
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
            Debug.Log("ATC: Nonpublic MethodInfo for RDNode");
            foreach (MethodInfo info in typeof(RDNode).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            { 
                string paramstr = "";
                foreach (ParameterInfo paramInfo in info.GetParameters())
                    paramstr += paramInfo.ToString() + ", ";

                Debug.Log("Name: " + info.ToString() + " params = " + paramstr);
            }

            Debug.Log("ATC: nonpublic MethodInfo for RDNode.Parent");
            foreach (MethodInfo info in typeof(RDNode.Parent).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                string paramstr = "";
                foreach (ParameterInfo paramInfo in info.GetParameters())
                    paramstr += paramInfo.ToString() + ", ";

                Debug.Log("Name: " + info.ToString() + " params = " + paramstr);
            }

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
                //GameEvents.onGameSceneLoadRequested.Add(new EventData<GameScenes>.OnEvent(OnGameSceneLoadRequested));
                GameEvents.OnTechnologyResearched.Add(new EventData<GameEvents.HostTargetAction<RDTech, RDTech.OperationResult>>.OnEvent(OnTechnologyResearched));
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
            if (!loadOnNextUpdate && Input.GetKeyDown(KeyCode.F8))
            {
                Debug.Log("Reloading Tree triggered");
                loadOnNextUpdate = true;
            }

            if (loadOnNextUpdate)
            {
                try
                {
                    LoadTree();                    
                }
                catch (Exception ex)
                {
                    Debug.Log("ATC: Error Loading tree - " + ex.Message + " at " + ex.StackTrace);
                }
                redrawArrows();
                loadOnNextUpdate = false;
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

        void OnTechnologyResearched(GameEvents.HostTargetAction<RDTech, RDTech.OperationResult> evt)
        {
            redrawArrows();
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


            Debug.Log("processing all MODIFY_NODE items");
            //check modify-nodes
            foreach (ConfigNode cfgNodeModify in tree.GetNodes("MODIFY_NODE")) //foreach (RDNode treeNode in GameObject.FindObjectsOfType(typeof(RDNode)).Cast<RDNode>())
            {
                string gameObjectName = cfgNodeModify.GetValue("gameObjectName");
                Debug.Log("processing MODIFY_NODE " + gameObjectName);
                RDNode treeNode = GameObject.FindObjectsOfType<RDNode>().Where(x => x.gameObject.name == gameObjectName).First();
                
                if (treeNode.treeNode)
                {
                    Debug.Log("found matching RDNode " + treeNode.gameObject.name);
                    updateNode(treeNode, cfgNodeModify);
                }//endif node found
            }//end for all nodes;

            
            //Debug.Log("processing all NEW_NODE items");
            //RDController rdControl = GameObject.FindObjectOfType<RDController>();
            //foreach (ConfigNode cfgNodeNew in tree.GetNodes("NEW_NODE"))
            //{
            //    RDNode newNode = createNode();
            //    newNode.treeNode = true;
            //    newNode.gameObject.name = cfgNodeNew.GetValue("gameObjectName");

            //    Debug.Log("Invoking rdController.registerNode");                
            //    rdControl.RegisterNode(newNode); //TODO maybe this needs to be done the other way around?

            //    Debug.Log("Invoking updateNode()");
            //    updateNode(newNode, cfgNodeNew);
                
            //}

        } //end loadTree()


        private void updateNode(RDNode treeNode, ConfigNode cfgNode)
        {
            if (cfgNode.HasValue("title"))
                treeNode.name = cfgNode.GetValue("title");


            if (cfgNode.HasValue("description"))
                treeNode.description = cfgNode.GetValue("description");


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

                }
                catch (Exception ex)
                {
                    Debug.LogError("Invalid Icon name '" + cfgNode.GetValue("icon") + "'" + ex.Message);
                    return;
                }

            }


            if (cfgNode.HasValue("anyParentUnlocks"))
                treeNode.AnyParentToUnlock = cfgNode.GetValue("anyParentUnlocks") == "true";



            //setup parent/child relations
            if (cfgNode.HasNode("REQUIRES"))
            {
                Debug.Log("ATC: changing/setting up tech dependencies for " + treeNode.gameObject.name);

                ConfigNode requiresNode = cfgNode.GetNode("REQUIRES");
                //clear all old parents
                //Assuming this also destroys all arrows
                foreach (RDNode.Parent oldParent in treeNode.parents)
                {
                    oldParent.parent.node.children.Remove(treeNode);
                    Vector.DestroyLine(ref oldParent.line);
                    GameObject.Destroy(oldParent.arrowHead);                    
                }
                Array.Clear(treeNode.parents, 0, treeNode.parents.Count());

                List<RDNode.Parent> parentList = new List<RDNode.Parent>();
                foreach (string parentName in requiresNode.GetValues("tech"))
                {
                    Debug.Log("technodename = " + parentName);
                    
                    RDNode parentNode = GameObject.FindObjectsOfType<RDNode>().Where(x => x.gameObject.name == parentName).First();
                    Debug.Log("found matching RDNode - " + parentNode.gameObject.name);
                    parentNode.children.Add(treeNode);

                    //create RDNode.Parent structure 
                    RDNode.Anchor anchorAtParent = RDNode.Anchor.LEFT;
                    RDNode.Anchor anchorAtNode = RDNode.Anchor.RIGHT;
                    RDNode.Parent parentStruct = new RDNode.Parent(new RDNode.ParentAnchor(parentNode, anchorAtParent), anchorAtNode);

                    
                    parentList.Add(parentStruct);
                }
                treeNode.parents = parentList.ToArray();
            }

            if (cfgNode.HasValue("posX") || cfgNode.HasValue("posY"))
            {
                Vector3 newPos = treeNode.transform.localPosition;
                Debug.Log("Old Position of Node " + treeNode.gameObject.name + " = " + newPos.ToString());

                if (cfgNode.HasValue("posX"))
                    newPos.x = float.Parse(cfgNode.GetValue("posX"));
                if (cfgNode.HasValue("posY"))
                    newPos.y = float.Parse(cfgNode.GetValue("posY"));

                treeNode.transform.localPosition = newPos;
            }

            

            //now get all parts
            Debug.Log("checking PARTS");
            if (cfgNode.HasNode("PARTS"))
            {
                treeNode.tech.partsAssigned.Clear();

                ConfigNode partsNode = cfgNode.GetNode("PARTS");
                Debug.Log("subnode PARTS has " + partsNode.GetValues("part").Count() + " part entries");

                foreach (string partString in partsNode.GetValues("part"))
                {
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

            
        }

        private void redrawArrows()
        {
            Debug.Log("Drawing all arrows");
            RDGridArea gridArea = GameObject.FindObjectOfType<RDController>().gridArea;
            Debug.Log("iterating over all RDNodes");
            foreach (RDNode rdNode in GameObject.FindObjectsOfType<RDNode>())
            {
                if (rdNode.state != RDNode.State.HIDDEN)
                {
                    Debug.Log("recreating incoming arrows for " + rdNode.gameObject.name);
                    Debug.Log("node has " + rdNode.parents.Count() + " parents");
                    for (int i = 0; i < rdNode.parents.Count(); ++i)
                    {
                        //Recreate Parent hopefully recreates incoming array? nope, doesnt, also not with calling UpdateGraphics and/or Setup not...
                        //just changing the line does not update the graphics either.
                        RDNode.Parent parentStruct = rdNode.parents[i];
                        if (parentStruct.line != null)
                            Vector.DestroyLine(ref parentStruct.line);
                        if (parentStruct.arrowHead != null)
                            GameObject.Destroy(parentStruct.arrowHead);

                        setAnchors(rdNode, ref parentStruct);
                    }//endfor foreach parentnode
                    //typeof(RDNode).GetMethod("InitializeArrows", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(treeNode, new object[] { });
                    Debug.Log("Redrawing incoming arrows");
                    if (rdNode.state == RDNode.State.RESEARCHED || rdNode.state == RDNode.State.RESEARCHABLE)
                        typeof(RDNode).GetMethod("DrawArrow", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(rdNode, new object[] { gridArea.LineMaterial });
                    else
                        typeof(RDNode).GetMethod("DrawArrow", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(rdNode, new object[] { gridArea.LineMaterialGray });

                }//endif hidden

                rdNode.UpdateGraphics();
            }//endforall rdnodes
        }

        private bool isAnchorAvailableForOutgoingArrows(RDNode node, RDNode.Anchor anchor)
        {
            foreach (RDNode.Parent incomingConnection in node.parents)
                if (incomingConnection.anchor == anchor)
                    return false;

            return true;
        }

        private void setAnchors(RDNode connectionOwner, ref RDNode.Parent connection)
        {
            Debug.Log("setting up anchors for connection " + connection.parent.node.gameObject.name + "->"+connectionOwner.gameObject.name);
            //find main direction from outgoing node (parent) to target (connectionOwner) node to set anchor tags
            //Exception: Cannot display incoming and outgoing nodes on the same anchor
            Vector3 connectionVec = connectionOwner.transform.localPosition - connection.parent.node.transform.localPosition;

            List<RDNode.Anchor> possibleParentAnchors = new List<RDNode.Anchor>();

            if (connectionVec.x >= 0) //left to right
                possibleParentAnchors.Add(RDNode.Anchor.RIGHT);
            else
                possibleParentAnchors.Add(RDNode.Anchor.LEFT);

            if (connectionVec.y >= 0) //up
                possibleParentAnchors.Add(RDNode.Anchor.TOP);
            else
                possibleParentAnchors.Add(RDNode.Anchor.BOTTOM);

            
            //filter by availability constraints from parent incoming occupied anchors
            if (!isAnchorAvailableForOutgoingArrows(connection.parent.node, possibleParentAnchors[1]))
                possibleParentAnchors.RemoveAt(1);
            if (!isAnchorAvailableForOutgoingArrows(connection.parent.node, possibleParentAnchors[0]))
                possibleParentAnchors.RemoveAt(0);

            Debug.Log("options remaining after filtering: " + possibleParentAnchors.Count());

            //if two options are available, pick the larger distance
            RDNode.Anchor selectedParentAnchor = RDNode.Anchor.RIGHT; //default fallback
            if (possibleParentAnchors.Count == 0)
                Debug.LogWarning("no valid anchor for outgoing arrow found!");
            else if (possibleParentAnchors.Count == 1)
                selectedParentAnchor = possibleParentAnchors.ElementAt(0);
            else
            {
                if (Math.Abs(connectionVec.x) > Math.Abs(connectionVec.y)) //left-right connection, prefer first
                    selectedParentAnchor = possibleParentAnchors.ElementAt(0);
                else
                    selectedParentAnchor = possibleParentAnchors.ElementAt(1);
            }


            connection.parent.anchor = selectedParentAnchor;
            if (selectedParentAnchor == RDNode.Anchor.BOTTOM)
                connection.anchor = RDNode.Anchor.TOP;
            else if (selectedParentAnchor == RDNode.Anchor.TOP)
                connection.anchor = RDNode.Anchor.BOTTOM;
            else if (selectedParentAnchor == RDNode.Anchor.LEFT)
                connection.anchor = RDNode.Anchor.RIGHT;
            else if (selectedParentAnchor == RDNode.Anchor.RIGHT)
                connection.anchor = RDNode.Anchor.LEFT;

            Debug.Log("Selected start anchor " + selectedParentAnchor);
        }

        private RDNode createNode()
        {
            Debug.Log("creating new RDNode by copiing the start-node");

            RDNode startNode = GameObject.FindObjectsOfType<RDNode>().Where(x => x.gameObject.name == "node0_start").First();

            GameObject nodeObject = (GameObject) GameObject.Instantiate(startNode);
            nodeObject.GetComponent<RDTech>().techID = "newTech";
            nodeObject.GetComponent<RDNode>().prefab = null;
            nodeObject.GetComponent<RDNode>().parents = new RDNode.Parent[0];
            Debug.Log("initializing icon and controller");
            nodeObject.GetComponent<RDNode>().icon = RDNode.Icon.GENERIC;
            nodeObject.GetComponent<RDNode>().tech.scienceCost = 100;

            nodeObject.GetComponent<RDNode>().controller = startNode.controller;
            nodeObject.GetComponent<RDNode>().scale = startNode.scale;

            nodeObject.SetActive(true);
            Debug.Log("setting transform parent and position");
            nodeObject.transform.parent = startNode.transform.parent;
            nodeObject.transform.localPosition =  new Vector3(0, 0, 0);
            nodeObject.GetComponent<RDNode>().children = new List<RDNode>();
            Debug.Log("created new node!");
            return nodeObject.GetComponent<RDNode>();               
        
        }

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



