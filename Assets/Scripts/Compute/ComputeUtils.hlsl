#include "Assets/Scripts/CreateLiquid.cs.hlsl"

//assumes w is a homoegenous coord
float3 cameraToViewPosition(float4 cameraPos)
{
    float3 viewPos =  cameraPos.xyz / cameraPos.w;
    viewPos = (viewPos + 1) / 2; //from -1,1 to 0,1;
    return viewPos;
}

float3 GetScreenNormal(float2 clipPoint, in ScreenParticle screenParticle)
{
    float3 viewPos = cameraToViewPosition(screenParticle.CameraPosition);
    float distFromCenter = distance(viewPos, clipPoint);
    float2 toEdge = clipPoint - viewPos;
    float2 toEdgeDir = normalize(toEdge);
    float3 tangent = float3(toEdgeDir.x, toEdgeDir.y, 0);
    float3 ortho = float3(0, 0, 1);

    float distFromCenterNorm = distFromCenter / screenParticle.Radius;

    //todo we need to acos interp it I think not just a lerp it to be correct for spheres
    float3 normal = lerp(ortho, tangent, distFromCenterNorm);
    return normalize(normal);
}

//remember to renormalize at the end!
void weightedAdd(inout ScreenCell cell, in ScreenCell other, float addedWeight)
{
    ScreenCell otherCell;
    //if neighbors cell is alpha (implying it is not a liquid cell)
    //don't blend into it... blend with yourself instead (effectively no change)
    if (other.Alpha > 0)
    {
        otherCell = other;
    } else
    {
        otherCell = cell;
    }
        //this breaks normal... must be renormalized at the end
        cell.NearestNormal = lerp(cell.NearestNormal, otherCell.NearestNormal, addedWeight);
    
        cell.Alpha += otherCell.Alpha * addedWeight;
        cell.FurthestParticle += otherCell.FurthestParticle * addedWeight;
        cell.NearestParticle += otherCell.NearestParticle * addedWeight;
        cell.NearestDepth += otherCell.NearestDepth * addedWeight;
    
}

//this isn't technically a box blur because the kernal is not currently shaped like a box... it has 8 pixels missing
//[x][ ][x][ ][x]
//[ ][x][x][x][ ]
//[x][x][x][x][x]
//[ ][x][x][x][ ]
//[x][ ][x][ ][x]
ScreenCell boxBlurScreenCell(int2 index, int2 cellsDimensions, RWStructuredBuffer<ScreenCell> cellsSrc)
{
    int indexFlat = index.x + index.y * cellsDimensions.x;
    int horizontalIncrement = 1;
    int verticalIncrement = cellsDimensions.x;

    ScreenCell cell;
    cell.Alpha = 0;
    cell.NearestNormal = float3(0,0,0);
    cell.FurthestParticle = 0;
    cell.NearestParticle = 0;
    cell.NearestDepth = 0;
    
    float selfWeight = 0.16;
    float manhatten1Weight = 0.06;
    float manhatten2Weight = 0.05; //58
    float diagonalWeight1 = 0.05; // 74
    float diagonalWeight2 = 0.05;

    weightedAdd(cell, cellsSrc[indexFlat], selfWeight);
    
    //manhatten nearest
    weightedAdd(cell, cellsSrc[indexFlat + horizontalIncrement], manhatten1Weight);
    weightedAdd(cell, cellsSrc[indexFlat - horizontalIncrement], manhatten1Weight);
    weightedAdd(cell, cellsSrc[indexFlat + verticalIncrement], manhatten1Weight);
    weightedAdd(cell, cellsSrc[indexFlat - verticalIncrement], manhatten1Weight);

    //diagonal nearest
    weightedAdd(cell, cellsSrc[indexFlat + horizontalIncrement + verticalIncrement], diagonalWeight1);
    weightedAdd(cell, cellsSrc[indexFlat - horizontalIncrement + verticalIncrement], diagonalWeight1);
    weightedAdd(cell, cellsSrc[indexFlat + horizontalIncrement - verticalIncrement], diagonalWeight1);
    weightedAdd(cell, cellsSrc[indexFlat - horizontalIncrement - verticalIncrement], diagonalWeight1);

    //manhatten non nearest
    weightedAdd(cell, cellsSrc[indexFlat + horizontalIncrement * 2], manhatten2Weight);
    weightedAdd(cell, cellsSrc[indexFlat - horizontalIncrement * 2], manhatten2Weight);
    weightedAdd(cell, cellsSrc[indexFlat + verticalIncrement * 2], manhatten2Weight);
    weightedAdd(cell, cellsSrc[indexFlat - verticalIncrement * 2], manhatten2Weight);

    //diagonal non nearest
    weightedAdd(cell, cellsSrc[indexFlat + (horizontalIncrement + verticalIncrement) * 2], diagonalWeight2);
    weightedAdd(cell, cellsSrc[indexFlat - (horizontalIncrement + verticalIncrement) * 2], diagonalWeight2);
    weightedAdd(cell, cellsSrc[indexFlat + (horizontalIncrement - verticalIncrement) * 2], diagonalWeight2);
    weightedAdd(cell, cellsSrc[indexFlat - (horizontalIncrement - verticalIncrement) * 2], diagonalWeight2);

    ScreenCell src = cellsSrc[indexFlat];
    cell.NearestParticle = src.NearestParticle;
    cell.FurthestParticle = src.FurthestParticle;
    cell.NearestNormal = normalize(cell.NearestNormal);
    return cell;
}