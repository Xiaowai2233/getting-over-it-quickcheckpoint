public static class CheckpointInputPolicy {
 public static bool ConfirmPressed(bool confirmationOpen,bool rebinding,bool keyDown,int openedFrame,int currentFrame) {
  return confirmationOpen && !rebinding && keyDown && currentFrame>openedFrame;
 }
}
