Shader "Custom/Transparent Bumped Diffuse" {
	Properties {
	    _Color ("Main Color", Color) = (1,1,1,1)
	    _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		_BumpMap ("Normalmap", 2D) = "bump" {}
	}
	SubShader {
	    Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	    LOD 200
		
	    // extra pass that renders to depth buffer only
	    Pass {
	        ZWrite On
	        ColorMask 0
	    }

	    // paste in forward rendering passes from Transparent/Diffuse
	    UsePass "Transparent/Bumped Diffuse/FORWARD"
	}
	
	Fallback "Bumped Diffuse"
}
