from app.schemas import ReadinessRequest
from app.tools.readiness_rules import validate_readiness


class ApplicationManagementAgent:
    """Uses only the approved readiness tool and returns structured observations."""

    def assess(self, snapshot: ReadinessRequest):
        return validate_readiness(snapshot)
