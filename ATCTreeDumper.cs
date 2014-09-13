using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ATC
{
    class ATCTreeDumper
    {
        public const bool m_bIsEnabled = false; // set to true in order to create new stock tree .cfg file.  Should only be necessary once with each vanilla release

        static public bool m_bHasTreeAlreadyBeenDumped = false;

        static public void DumpCurrentTreeToFile( string sFileName, string sTreeName )
        {
            // only attempt to dump if the current game mode has a tech tree, and the tech tree is present

            if ( HighLogic.CurrentGame != null && ( HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX )
                && AssetBase.RnDTechTree != null && AssetBase.RnDTechTree.GetTreeNodes() != null )
            {
			    ConfigNode fileConfigNode = new ConfigNode();

			    ConfigNode treeConfigNode = new ConfigNode( "TECH_TREE" );

                treeConfigNode.AddValue( "name", sTreeName );

                AddAllTechNodesToTreeNode( treeConfigNode );

                AddPlanetScienceValuesToTreeNode( treeConfigNode );

                fileConfigNode.AddNode( treeConfigNode );

			    fileConfigNode.Save( KSPUtil.ApplicationRootPath.Replace( "\\", "/" ) + "GameData/ATC/" + sFileName, 
                    "Config file representing the stock tech tree\r\n" +
                    "// WARNING: This file should not be edited directly, but rather should either be altered using ModuleManager commands within your own .cfg files OR\r\n" +
                    "// a new tree .cfg should be created which settings.cfg can then be set to point to." );

                m_bHasTreeAlreadyBeenDumped = true;
            }
        }

        static private void AddAllTechNodesToTreeNode( ConfigNode treeConfigNode )
        {
            List<RDNode> nodesAlreadyProcessed = new List<RDNode>();

            // order the output of tree nodes so that they're easier to reference in the resulting file

            for ( int iTempTechLevel = 0; iTempTechLevel < 10; iTempTechLevel++ )
            {
                string sFilterString = "node" + iTempTechLevel + "_";

                foreach ( RDNode tempRDNode in AssetBase.RnDTechTree.GetTreeNodes() )                    
                {
                    if ( tempRDNode != null && tempRDNode.gameObject.name.StartsWith( sFilterString ) && !nodesAlreadyProcessed.Contains( tempRDNode ) )
                    {
                        AddTechNodeToTreeNode( tempRDNode, treeConfigNode );

                        nodesAlreadyProcessed.Add( tempRDNode );
                    }
                }
            }

            // final pass in case there are any leftover nodes defined that don't follow the standard naming convention
				
            foreach ( RDNode tempRDNode in AssetBase.RnDTechTree.GetTreeNodes() )                    
            {
                if ( tempRDNode != null && !nodesAlreadyProcessed.Contains( tempRDNode ) )
                {
                    AddTechNodeToTreeNode( tempRDNode, treeConfigNode );

                    nodesAlreadyProcessed.Add( tempRDNode );
                }
            }
        }

        static private void AddPlanetScienceValuesToTreeNode( ConfigNode treeConfigNode )
        {
            foreach ( CelestialBody tempBody in FlightGlobals.Bodies )
			{
                ConfigNode bodyConfigNode = new ConfigNode( "BODY_SCIENCE_PARAMS" );

                bodyConfigNode.AddValue( "name", tempBody.name );

	            bodyConfigNode.AddValue( "LandedDataValue", tempBody.scienceValues.LandedDataValue );
                bodyConfigNode.AddValue( "SplashedDataValue", tempBody.scienceValues.SplashedDataValue );

                bodyConfigNode.AddValue( "FlyingLowDataValue", tempBody.scienceValues.FlyingLowDataValue );
                bodyConfigNode.AddValue( "FlyingHighDataValue", tempBody.scienceValues.FlyingHighDataValue );

                bodyConfigNode.AddValue( "InSpaceLowDataValue", tempBody.scienceValues.InSpaceLowDataValue );
                bodyConfigNode.AddValue( "InSpaceHighDataValue", tempBody.scienceValues.InSpaceHighDataValue );

                bodyConfigNode.AddValue( "flyingAltitudeThreshold", tempBody.scienceValues.flyingAltitudeThreshold );
                bodyConfigNode.AddValue( "spaceAltitudeThreshold" , tempBody.scienceValues.spaceAltitudeThreshold );

                bodyConfigNode.AddValue( "RecoveryValue", tempBody.scienceValues.RecoveryValue );

                treeConfigNode.AddNode( bodyConfigNode );
            }
        }

        static private void AddTechNodeToTreeNode( RDNode techNode, ConfigNode treeConfigNode )
        {
            ConfigNode techNodeConfigNode = new ConfigNode( "TECH_NODE" );

            techNodeConfigNode.AddValue( "name", techNode.gameObject.name );

            techNodeConfigNode.AddValue( "title", techNode.tech.title );
            techNodeConfigNode.AddValue( "description", techNode.tech.description );
            techNodeConfigNode.AddValue( "icon", techNode.icon.ToString());

            techNodeConfigNode.AddValue( "scienceCost", techNode.tech.scienceCost );

            techNodeConfigNode.AddValue( "anyParentUnlocks", techNode.AnyParentToUnlock );

            techNodeConfigNode.AddValue( "hideIfNoparts", techNode.hideIfNoParts );

            techNodeConfigNode.AddValue( "posX", techNode.transform.localPosition.x );
            techNodeConfigNode.AddValue( "posY", techNode.transform.localPosition.y );

            if ( techNode.parents != null )
            {
                foreach ( RDNode.Parent tempParentConnection in techNode.parents )
                {
                    if ( tempParentConnection != null )
                    {
                        ConfigNode parentConfigNode = new ConfigNode( "PARENT_NODE" );

                        parentConfigNode.AddValue( "name", tempParentConnection.parent.node.gameObject.name );

                        techNodeConfigNode.AddNode( parentConfigNode );
                    }
                }
            }

            treeConfigNode.AddNode( techNodeConfigNode );
        }
    }
}
