// This algorithm is basically the Simple Stupid Funnel Algorithm posted by
// Mikko in the Digesting Duck blog. This one has been modified to account for agent radius.
static public void Funnel(float radius, List<HalfEdgeVertex> portals, ref List<Apex> contactVertices) {
    // In some special cases, it is possible that the tangents of the apexes will
    // cause the funnel to collapse to the left or right portal right before going to
    // the final position. This happens when the final position is more 'outward' than
    // the vector from the apex to the portal extremity, and the final position is
    // actually 'closer' to the previous portal than the 'current' portal extremity.
    // If that happens, we remove the portal before the last from the list. I have no
    // proof that this guarantees the correct behavior, though.
    if(portals.Count >= 8) {
        // This seems to be possible to happen only when there are 4 or more
        // portals (first and last are start and destination)
        int basePortal = portals.Count - 6;
        int lastPortal = portals.Count - 4;
        int destinationPortal = portals.Count - 2;

        // First, check left
        Vector2 baseLast = portals[lastPortal].position - portals[basePortal].position;
        Vector2 baseDest = portals[destinationPortal].position - portals[basePortal].position;
        if(baseDest.sqrMagnitude < baseLast.sqrMagnitude) {
            portals.RemoveRange(lastPortal, 2);
        } else {
            // Now check right
            baseLast = portals[lastPortal+1].position - portals[basePortal+1].position;
            baseDest = portals[destinationPortal+1].position - portals[basePortal+1].position;
            if(baseDest.sqrMagnitude < baseLast.sqrMagnitude) {
                portals.RemoveRange(lastPortal, 2);
            }
        }
    }


    HalfEdgeVertex portalApex = portals[0];
    HalfEdgeVertex portalLeft = portals[0];
    HalfEdgeVertex portalRight = portals[1];
    
    int portalLeftIndex = 0;
    int portalRightIndex = 0;
    
    // Put the first point into the contact list
    Apex startApex = new Apex();
    startApex.vertex = portalApex;
    startApex.type = ApexTypes.Point;
    
    contactVertices.Clear();
    contactVertices.Add(startApex);
    
    ApexTypes currentType = ApexTypes.Point;
    Vector2 previousValidLSegment = Vector2.zero;
    Vector2 previousValidRSegment = Vector2.zero;
    
    
    
    for(int i = 2; i < portals.Count; i += 2) {
        HalfEdgeVertex left = portals[i];
        HalfEdgeVertex right = portals[i+1];

        ApexTypes nextLeft = ApexTypes.Left;
        ApexTypes nextRight = ApexTypes.Right;
        if(i >= portals.Count - 2) {
            // Correct next apex type if we are at the end of the channel
            nextLeft = ApexTypes.Point;
            nextRight = ApexTypes.Point;
        }

        // Build radius-inflated line segments
        Vector2 tempA = portalApex.position, tempB = left.position;
        GetTangentPoints(
            tempA, tempB, currentType, nextLeft, radius, out tempA, out tempB
        );
        Vector2 currentLSegment = tempB - tempA;

        tempA = portalApex.position; tempB = right.position;
        GetTangentPoints(
            tempA, tempB, currentType, nextRight, radius, out tempA, out tempB
        );
        Vector2 currentRSegment = tempB - tempA;
        
        
        //Right side
        // Does new 'right' reduce the funnel?
        if(MyMath2D.CrossProduct2D(previousValidRSegment, currentRSegment) > -MyMath2D.tolerance) {
            // Does it NOT cross the left side?
            // Is the apex the same as portal right? (if true, no chance but to move)
            if(
                portalApex == portalRight ||
                MyMath2D.CrossProduct2D(previousValidLSegment, currentRSegment) < MyMath2D.tolerance
            ) {
                portalRight = right;
                previousValidRSegment = currentRSegment;
                portalRightIndex = i;
            } else {
                // Collapse
                if(currentRSegment.sqrMagnitude > previousValidLSegment.sqrMagnitude) {
                    portalApex = portalLeft;
                    portalRight = portalApex;
                    
                    Apex apex = new Apex();
                    apex.vertex = portalApex;
                    apex.type = ApexTypes.Left;
                    contactVertices.Add(apex);

                    currentType = ApexTypes.Left;
                    
                    portalRightIndex = portalLeftIndex;
                    i = portalLeftIndex;
                } else {
                    portalRight = right;
                    previousValidRSegment = currentRSegment;
                    portalRightIndex = i;

                    portalApex = portalRight;
                    portalLeft = portalApex;
                    
                    Apex apex = new Apex();
                    apex.vertex = portalApex;
                    apex.type = ApexTypes.Right;
                    contactVertices.Add(apex);
                    
                    currentType = ApexTypes.Right;
                    
                    portalLeftIndex = portalRightIndex;
                    i = portalRightIndex;
                }

                previousValidLSegment = Vector2.zero;
                previousValidRSegment = Vector2.zero;

                continue;
            }
        }
        
        // Left Side
        // Does new 'left' reduce the funnel?
        if(MyMath2D.CrossProduct2D(previousValidLSegment, currentLSegment) < MyMath2D.tolerance) {
            // Does it NOT cross the right side?
            // Is the apex the same as portal left? (if true, no chance but to move)
            if(
                portalApex == portalLeft ||
                MyMath2D.CrossProduct2D(previousValidRSegment, currentLSegment) > -MyMath2D.tolerance
            ) {
                portalLeft = left;
                previousValidLSegment = currentLSegment;
                portalLeftIndex = i;
            } else {
                // Collapse
                if(currentLSegment.sqrMagnitude > previousValidRSegment.sqrMagnitude) {
                    portalApex = portalRight;
                    portalLeft = portalApex;
                    
                    Apex apex = new Apex();
                    apex.vertex = portalApex;
                    apex.type = ApexTypes.Right;
                    contactVertices.Add(apex);

                    currentType = ApexTypes.Right;
                    
                    portalLeftIndex = portalRightIndex;
                    i = portalRightIndex;
                } else {
                    portalLeft = left;
                    previousValidLSegment = currentLSegment;
                    portalLeftIndex = i;

                    portalApex = portalLeft;
                    portalRight = portalApex;
                    
                    Apex apex = new Apex();
                    apex.vertex = portalApex;
                    apex.type = ApexTypes.Left;
                    contactVertices.Add(apex);
                    
                    currentType = ApexTypes.Left;
                    
                    portalRightIndex = portalLeftIndex;
                    i = portalLeftIndex;
                }

                previousValidLSegment = Vector2.zero;
                previousValidRSegment = Vector2.zero;

                continue;
            }
        }
    }
    
    // Put the last point into the contact list
    if(contactVertices[contactVertices.Count - 1].vertex == portals[portals.Count-1]) {
        // Last point was added to funnel, so we need to change its type to point
        Apex endApex = new Apex();
        endApex.vertex = portals[portals.Count-1];
        endApex.type = ApexTypes.Point;
        contactVertices[contactVertices.Count-1] = endApex;
    } else {
        // Last point was not added to funnel, so we add it
        Apex endApex = new Apex();
        endApex.vertex = portals[portals.Count - 1];
        endApex.type = ApexTypes.Point;
        contactVertices.Add(endApex);
    }
}

static public void BuildPath(float radius, List<Apex> contactVertices, ref List<Vector2> path) {
    path.Clear();

    // My channel actually goes from path end to path start, so I need to
    // invert all the apexes sides...

    // Add first node
    path.Add(contactVertices[contactVertices.Count - 1].vertex.position);
    
    for(int i = contactVertices.Count - 2; i >= 0; --i) {
        Vector2 positionA = Vector2.zero, positionB = Vector2.zero;

        ApexTypes invertedA = contactVertices[i+1].type;
        ApexTypes invertedB = contactVertices[i].type;

        if(invertedA == ApexTypes.Left) invertedA = ApexTypes.Right;
        else if(invertedA == ApexTypes.Right) invertedA = ApexTypes.Left;

        if(invertedB == ApexTypes.Left) invertedB = ApexTypes.Right;
        else if(invertedB == ApexTypes.Right) invertedB = ApexTypes.Left;

        GetTangentPoints(
            contactVertices[i+1].vertex.position, contactVertices[i].vertex.position,
            invertedA, invertedB, radius, out positionA, out positionB
        );
        path.Add(positionA);
        path.Add(positionB);
    }
}

