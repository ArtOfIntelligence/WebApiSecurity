Imports System.Net
Imports System.Net.Http
Imports System.Security.Principal
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Web.Http
Imports System.Web.Http.Filters

Namespace Server

    Public Class AuthenticationFilterAttribute
        Inherits Attribute
        Implements IAuthenticationFilter

        Public ReadOnly Property AllowMultiple As Boolean Implements IFilter.AllowMultiple
            Get
                Return True
            End Get
        End Property

        Public Async Function AuthenticateAsync(context As HttpAuthenticationContext, cancellationToken As CancellationToken) As Task Implements IAuthenticationFilter.AuthenticateAsync
            ' 1. Look for credentials in the request.
            Dim req = context.Request
            Dim authorization = req.Headers.Authorization

            ' 2. If there are no credentials, do nothing.
            If authorization Is Nothing Then
                context.ErrorResult = New AuthenticationFailureResult("Authorization header is missing", req)
                Exit Function
            End If

            ' 3. If there are credentials but the filter does Not recognize the 
            '    authentication scheme, do nothing.
            If authorization.Scheme.ToLower <> Configurator.AuthorizationScheme.ToLower Then
                context.ErrorResult =
                    New AuthenticationFailureResult("Invalid Authorization scheme. Expected: Authorization: " & Configurator.AuthorizationScheme & " <tokenKey>", req)
                Exit Function
            End If

            ' 4. If there are credentials that the filter understands, try to validate them.
            ' 5. If the credentials are bad, set the error result.
            If String.IsNullOrEmpty(authorization.Parameter) Then
                context.ErrorResult = New AuthenticationFailureResult("Authorization key is missing. Expected: Authorization: " & Configurator.AuthorizationScheme & " <tokenKey>", req)
                Exit Function
            End If

            Dim tokenString = authorization.Parameter
            If tokenString Is Nothing Then
                context.ErrorResult = New AuthenticationFailureResult("Authorization key is missing. Expected: Authorization: " & Configurator.AuthorizationScheme & " <tokenKey>", req)
                Exit Function
            Else
                Dim token = Model.TokenKey.TryDeserialize(tokenString, Configurator.InternalCipherKey)
                If token Then
                    If Now > token.Result.ExpiryDate Then
                        context.ErrorResult = New AuthenticationFailureResult("Token Expired", req)
                    Else
                        context.Principal = New Model.ClientPrincipal(token.Result.ClientId)
                        Thread.CurrentPrincipal = context.Principal
                    End If
                Else
                    context.ErrorResult = New AuthenticationFailureResult("Key format is invalid", req)
                End If
            End If

        End Function

        Public Async Function ChallengeAsync(context As HttpAuthenticationChallengeContext, cancellationToken As CancellationToken) As Task Implements IAuthenticationFilter.ChallengeAsync
            'context.ActionCntext.Response
            'context.Result = New AuthenticationFailureResult("No Challenge", context.Request)

        End Function

    End Class

    Public Class AuthenticationFailureResult
        Implements IHttpActionResult

        Public ReadOnly Property ReasonPhrase As String
        Public ReadOnly Property Request As HttpRequestMessage

        Public Sub New(reasonPhrase As String, request As HttpRequestMessage)
            Me.ReasonPhrase = reasonPhrase
            Me.Request = request
        End Sub

        Public Function ExecuteAsync(cancellationToken As CancellationToken) As Task(Of HttpResponseMessage) Implements IHttpActionResult.ExecuteAsync
            Return Task.FromResult(Execute)
        End Function

        Private Function Execute() As HttpResponseMessage
            Dim response = New HttpResponseMessage(HttpStatusCode.Unauthorized) With {
                .RequestMessage = Request,
                .ReasonPhrase = ReasonPhrase
            }
            Return response
        End Function


    End Class

End Namespace
