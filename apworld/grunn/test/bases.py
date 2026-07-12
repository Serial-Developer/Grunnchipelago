from test.bases import WorldTestBase

from .. import GrunnWorld


class GrunnTestBase(WorldTestBase):
    game = "Grunn"
    world: GrunnWorld
