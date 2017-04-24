A RESTful API built using .NET Core. This API will be built to the highest possible level, level 3 maturity, based on the Richardson Maturity Model.

### Richardson Maturity Model

##### Level 0 - The Swamp of POX

Uses it's implementing protocol like a transport protocol. It uses one entry point URI and one kind of method. Similar to SOAP and XML-RPC.

##### Level 1 - Resources

Introduces Resources. Rather than making all our requests to a single URI endpoint we start talking to individual resources.

##### Level 2 - HTTP Verbs

In level 0 and level 1 HTTP POST verbs are used for the majority of interactions, however GETs can be used too as they are only being used to tunnel interactions. Level 2 moves away from this tunnelling style and uses the HTTP verbs as closely as possible to how they are used in HTTP itself.

##### Level 3 - Hypermedia

Level 3, the highest level introduces discoverability to our API by utilising HATEOS (Hypermedia As The Engine of Application State)   

Martin Fowler provides a detailed overview of the [Richardson Maturity Model](https://martinfowler.com/articles/richardsonMaturityModel.html).
