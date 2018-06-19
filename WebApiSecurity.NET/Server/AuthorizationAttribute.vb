Imports System.Threading
Imports System.Web.Http
Imports System.Web.Http.Controllers
Imports ArtOfIntelligence.WebApiSecurity.Model

Namespace Server

    Public Class AuthorizationAttribute
        Inherits AuthorizeAttribute

        Protected Overrides Function IsAuthorized(actionContext As HttpActionContext) As Boolean

            Dim cPrincipal = Thread.CurrentPrincipal

            If cPrincipal Is Nothing AndAlso
                actionContext IsNot Nothing AndAlso
                actionContext.RequestContext IsNot Nothing AndAlso
                actionContext.RequestContext.Principal IsNot Nothing AndAlso
                actionContext.RequestContext.Principal.Identity IsNot Nothing Then _
                cPrincipal = actionContext.RequestContext.Principal

            If cPrincipal IsNot Nothing AndAlso
                TypeOf (cPrincipal) Is ClientPrincipal AndAlso
                cPrincipal.Identity.IsAuthenticated Then

                Dim ClientPrincipal As ClientPrincipal = cPrincipal
                Return IsAutenticatedInRole(ClientPrincipal.ClientId)

            End If

            Return False

        End Function

        Private Function IsAutenticatedInRole(ClientId As String) As Boolean

            If Me.Roles.Length > 0 Then

                Dim userRoles = Configurator.GetClientRoles(ClientId)

                Dim allowedRoles = Split(Roles, (","))

                Dim matches = userRoles.Intersect(allowedRoles).ToArray()

                Return matches.Length > 0
            End If

            Return True
        End Function

    End Class



End Namespace