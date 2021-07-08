// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.WebRTC.Unity
{
    /// <summary>
    /// Abstract base class to simplify implementing a WebRTC signaling solution in Unity.
    /// 
    /// There is no requirement to use this class as a base class for a custom implementation,
    /// but it handles automatically registering the necessary <see cref="Unity.PeerConnection"/>
    /// event handlers, as well as dispatching free-threaded callbacks to the main Unity app thread
    /// for simplicity and safety, and leaves the implementation with instead with two sending methods
    /// <see cref="SendMessageAsync(SdpMessage)"/> and <see cref="SendMessageAsync(IceCandidate)"/> to
    /// implement, as well as handling received messages.
    /// </summary>
    public abstract class Signaler : WorkQueue
    {
        /// <summary>
        /// The <see cref="PeerConnection"/> this signaler needs to work for.
        /// </summary>
        public PeerConnection PeerConnection;


        #region Signaler interface

        /// <summary>
        /// Asynchronously send an SDP message to the remote peer.
        /// </summary>
        /// <param name="message">The SDP message to send to the remote peer.</param>
        /// <returns>
        /// A <see cref="Task"/> object completed once the message has been sent,
        /// but not necessarily delivered.
        /// </returns>
        public abstract Task SendMessageAsync(SdpMessage message);

        /// <summary>
        /// Asynchronously send an ICE candidate to the remote peer.
        /// </summary>
        /// <param name="candidate">The ICE candidate to send to the remote peer.</param>
        /// <returns>
        /// A <see cref="Task"/> object completed once the message has been sent,
        /// but not necessarily delivered.
        /// </returns>
        public abstract Task SendMessageAsync(IceCandidate candidate);

        #endregion


        /// <summary>
        /// Native <xref href="Microsoft.MixedReality.WebRTC.PeerConnection"/> object from the underlying
        /// WebRTC C# library, available once the peer has been initialized.
        /// </summary>
        protected WebRTC.PeerConnection _nativePeer = null;

        /// <summary>
        /// Callback fired from the <see cref="PeerConnection"/> when it finished
        /// initializing, to subscribe to signaling-related events.
        /// </summary>
        /// <param name="peer">The peer connection to attach to</param>
        public void OnPeerInitialized()
        {
            _nativePeer = PeerConnection.Peer;

            // Register handlers for the SDP events
            _nativePeer.IceCandidateReadytoSend += OnIceCandidateReadyToSend_Listener;
            _nativePeer.LocalSdpReadytoSend += OnLocalSdpReadyToSend_Listener;
        }

        /// <summary>
        /// Callback fired from the <see cref="PeerConnection"/> before it starts
        /// uninitializing itself and disposing of the underlying implementation object.
        /// </summary>
        /// <param name="peer">The peer connection about to be deinitialized</param>
        public void OnPeerUninitializing()
        {
            // Unregister handlers for the SDP events
            //_nativePeer.IceCandidateReadytoSend -= OnIceCandidateReadyToSend_Listener;
            //_nativePeer.LocalSdpReadytoSend -= OnLocalSdpReadyToSend_Listener;
        }

        private void OnIceCandidateReadyToSend_Listener(IceCandidate candidate)
        {
            InvokeOnAppThread(() => OnIceCandidateReadyToSend(candidate));
        }

        /// <summary>
        /// Helper to split SDP offer and answer messages and dispatch to the appropriate handler.
        /// </summary>
        /// <param name="message">The SDP message ready to be sent to the remote peer.</param>
        private void OnLocalSdpReadyToSend_Listener(SdpMessage message)
        {
            if (message.Type == SdpMessageType.Offer)
            {
                InvokeOnAppThread(() => OnSdpOfferReadyToSend(message));
            }
            else if (message.Type == SdpMessageType.Answer)
            {
                InvokeOnAppThread(() => OnSdpAnswerReadyToSend(message));
            }
        }

        protected virtual void OnEnable()
        {
            PeerConnection.OnInitialized.AddListener(OnPeerInitialized);
            PeerConnection.OnShutdown.AddListener(OnPeerUninitializing);
        }

        protected virtual void OnDisable()
        {
            PeerConnection.OnInitialized.RemoveListener(OnPeerInitialized);
            PeerConnection.OnShutdown.RemoveListener(OnPeerUninitializing);
        }

        /// <summary>
        /// Callback invoked when an ICE candidate message has been generated and is ready to
        /// be sent to the remote peer by the signaling object.
        /// </summary>
        /// <param name="candidate">ICE candidate to send to the remote peer.</param>
        protected virtual void OnIceCandidateReadyToSend(IceCandidate candidate)
        {
            SendMessageAsync(candidate);
        }

        /// <summary>
        /// Callback invoked when a local SDP offer has been generated and is ready to
        /// be sent to the remote peer by the signaling object.
        /// </summary>
        /// <param name="offer">The SDP offer message to send.</param>
        protected virtual void OnSdpOfferReadyToSend(SdpMessage offer)
        {
            SendMessageAsync(offer);
        }

        /// <summary>
        /// Callback invoked when a local SDP answer has been generated and is ready to
        /// be sent to the remote peer by the signaling object.
        /// </summary>
        /// <param name="answer">The SDP answer message to send.</param>
        protected virtual void OnSdpAnswerReadyToSend(SdpMessage answer)
        {
            SendMessageAsync(answer);
        }
    }
}
